using Microsoft.Extensions.Options;
using Timekeeping.Agent.Configuration;
using Timekeeping.Agent.Services;
using Timekeeping.Agent.Storage;
using Timekeeping.Contracts;
using Microsoft.Extensions.Hosting;


namespace Timekeeping.Agent.Workers;

/// <summary>
/// Pushes buffered punches to HQ over the VPN with exponential backoff when the tunnel is down.
/// </summary>
public sealed class SyncWorker : BackgroundService
{
    private readonly IngestApiClient _client;
    private readonly LocalPunchStore _store;
    private readonly ZkDeviceService _device;
    private readonly AgentStatusHub _status;
    private readonly AgentOptions _options;
    private readonly ILogger<SyncWorker> _logger;

    private int _backoffSeconds;
    private DateTime _lastClockSyncDate = DateTime.MinValue;

    public SyncWorker(
        IngestApiClient client,
        LocalPunchStore store,
        ZkDeviceService device,
        AgentStatusHub status,
        IOptions<AgentOptions> options,
        ILogger<SyncWorker> logger)
    {
        _client = client;
        _store = store;
        _device = device;
        _status = status;
        _options = options.Value;
        _logger = logger;
        _backoffSeconds = Math.Max(1, _options.Sync.InitialBackoffSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _status.Update(s =>
        {
            s.SiteId = _options.SiteId;
            s.SyncStatus = "Starting";
        });

        // Stagger startup so device connect can settle.
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var reachable = await _client.IsReachableAsync(stoppingToken);
                _status.Update(s => s.VpnOrApiReachable = reachable);

                if (!reachable)
                {
                    _status.Update(s =>
                    {
                        s.SyncStatus = $"VPN/API down — retry in {_backoffSeconds}s";
                        s.LastError = "Ingest API unreachable (check VPN)";
                    });
                    RefreshCounts();
                    await Task.Delay(TimeSpan.FromSeconds(_backoffSeconds), stoppingToken);
                    _backoffSeconds = Math.Min(
                        _options.Sync.MaxBackoffSeconds,
                        Math.Max(_options.Sync.InitialBackoffSeconds, _backoffSeconds * 2));
                    continue;
                }

                _backoffSeconds = Math.Max(1, _options.Sync.InitialBackoffSeconds);
                await MaybeSyncDeviceClockAsync(stoppingToken);
                await UploadPendingAsync(stoppingToken);
                RefreshCounts();

                await Task.Delay(
                    TimeSpan.FromSeconds(Math.Max(5, _options.Sync.SyncIntervalSeconds)),
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sync loop error");
                _status.Update(s =>
                {
                    s.SyncStatus = "Error";
                    s.LastError = ex.Message;
                });
                await Task.Delay(TimeSpan.FromSeconds(_backoffSeconds), stoppingToken);
                _backoffSeconds = Math.Min(_options.Sync.MaxBackoffSeconds, _backoffSeconds * 2);
            }
        }
    }

    private async Task UploadPendingAsync(CancellationToken ct)
    {
        var batchSize = Math.Max(1, _options.Ingest.BatchSize);
        var pending = _store.GetRetryablePunches(batchSize);
        if (pending.Count == 0)
        {
            _status.Update(s => s.SyncStatus = "Idle — queue empty");
            return;
        }

        _status.Update(s => s.SyncStatus = $"Uploading {pending.Count} punch(es)");

        var deviceSn = string.IsNullOrWhiteSpace(_device.SerialNumber)
            ? pending.Select(p => p.DeviceSerialNumber).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)) ?? ""
            : _device.SerialNumber;

        var request = new PunchBatchRequest
        {
            SiteId = _options.SiteId,
            DeviceSerialNumber = deviceSn,
            Punches = pending.Select(p => new PunchDto
            {
                LocalId = p.Id,
                EnrollNumber = p.EnrollNumber,
                InOutMode = p.InOutMode,
                ModType = p.ModType,
                Timestamp = p.Timestamp,
                WorkCode = p.WorkCode,
                DeviceSerialNumber = string.IsNullOrWhiteSpace(p.DeviceSerialNumber) ? deviceSn : p.DeviceSerialNumber
            }).ToList()
        };

        var response = await _client.SendBatchAsync(request, ct);
        if (response is null)
        {
            _status.Update(s =>
            {
                s.VpnOrApiReachable = false;
                s.SyncStatus = "Upload failed — will backoff";
                s.LastError = "Batch upload failed (VPN/network)";
            });
            throw new IOException("Batch upload failed");
        }

        var updates = new List<(long Id, PunchSyncState State, string? Error)>();
        foreach (var result in response.Results)
        {
            if (result.LocalId is null) continue;

            var state = result.Status switch
            {
                PunchIngestStatus.Accepted => PunchSyncState.Synced,
                PunchIngestStatus.Duplicate => PunchSyncState.Synced,
                PunchIngestStatus.Unmapped => PunchSyncState.Unmapped,
                _ => PunchSyncState.Failed
            };

            updates.Add((result.LocalId.Value, state, result.Message));
        }

        // Any local rows missing from the response stay pending for retry.
        var returned = response.Results
            .Where(r => r.LocalId.HasValue)
            .Select(r => r.LocalId!.Value)
            .ToHashSet();
        foreach (var punch in pending.Where(p => !returned.Contains(p.Id)))
            updates.Add((punch.Id, PunchSyncState.Failed, "No result returned from ingest API"));

        _store.MarkResults(updates);

        var accepted = updates.Count(u => u.State == PunchSyncState.Synced);
        _logger.LogInformation(
            "Sync complete: accepted/dup={Accepted}, unmapped={Unmapped}, failed={Failed}",
            accepted,
            updates.Count(u => u.State == PunchSyncState.Unmapped),
            updates.Count(u => u.State == PunchSyncState.Failed));

        _status.Update(s =>
        {
            s.VpnOrApiReachable = true;
            s.LastSuccessfulSyncUtc = DateTime.UtcNow;
            s.SyncStatus = $"Last sync OK ({accepted} accepted/dup)";
            s.LastError = null;
            s.DeviceSerialNumber = deviceSn;
        });
    }

    private async Task MaybeSyncDeviceClockAsync(CancellationToken ct)
    {
        if (!_options.Sync.SyncDeviceClockDaily || !_device.IsConnected)
            return;

        if (!TimeSpan.TryParse(_options.Sync.DeviceClockSyncTimeLocal, out var target))
            target = new TimeSpan(2, 0, 0);

        var nowLocal = DateTime.Now;
        if (nowLocal.Date == _lastClockSyncDate.Date)
            return;

        if (nowLocal.TimeOfDay < target || nowLocal.TimeOfDay > target.Add(TimeSpan.FromMinutes(5)))
            return;

        var serverTime = await _client.GetServerTimeAsync(ct);
        if (serverTime is null)
            return;

        if (_device.TrySetDeviceTime(serverTime.Value))
        {
            _lastClockSyncDate = nowLocal.Date;
            _logger.LogInformation("Device clock synced to server time {ServerTime}", serverTime);
        }
    }

    private void RefreshCounts()
    {
        var (pending, failed, unmapped) = _store.GetCounts();
        _status.Update(s =>
        {
            s.PendingCount = pending;
            s.FailedCount = failed;
            s.UnmappedCount = unmapped;
            s.SiteId = _options.SiteId;
        });
    }
}
