using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using Timekeeping.Agent.Configuration;
using Timekeeping.Agent.Storage;

namespace Timekeeping.Agent.Services;

/// <summary>
/// Owns a single zkemkeeper COM instance on a dedicated STA thread.
/// </summary>
public sealed class ZkDeviceService : IHostedService, IDisposable
{
    private readonly AgentOptions _options;
    private readonly LocalPunchStore _store;
    private readonly AgentStatusHub _status;
    private readonly ILogger<ZkDeviceService> _logger;

    private Thread? _staThread;
    private ZkComDevice? _zk;
    private readonly AutoResetEvent _wake = new(false);
    private readonly ManualResetEventSlim _started = new(false);
    private volatile bool _shouldRun;
    private bool _connected;
    private string _serialNumber = "";
    private DateTime _watermark = DateTime.MinValue;

    public ZkDeviceService(
        IOptions<AgentOptions> options,
        LocalPunchStore store,
        AgentStatusHub status,
        ILogger<ZkDeviceService> logger)
    {
        _options = options.Value;
        _store = store;
        _status = status;
        _logger = logger;
    }

    public string SerialNumber => _serialNumber;
    public bool IsConnected => _connected;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _store.EnsureCreated();
        _watermark = _store.GetWatermark() ?? DateTime.MinValue;

        _shouldRun = true;
        _staThread = new Thread(StaLoop)
        {
            IsBackground = true,
            Name = "ZkDevice-STA"
        };
        _staThread.SetApartmentState(ApartmentState.STA);
        _staThread.Start();
        _started.Wait(TimeSpan.FromSeconds(5));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _shouldRun = false;
        _wake.Set();
        _staThread?.Join(TimeSpan.FromSeconds(5));
        return Task.CompletedTask;
    }

    public void RequestPoll() => _wake.Set();

    public bool TrySetDeviceTime(DateTime localTime)
    {
        if (!_connected || _zk is null)
            return false;

        try
        {
            return _zk.SetDeviceTime2(
                _options.Device.MachineNumber,
                localTime.Year,
                localTime.Month,
                localTime.Day,
                localTime.Hour,
                localTime.Minute,
                localTime.Second);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set device clock");
            return false;
        }
    }

    private void StaLoop()
    {
        try
        {
            if (!ZkComDevice.TryCreate(out _zk, out var error) || _zk is null)
            {
                _logger.LogError("zkemkeeper unavailable: {Error}", error);
                _status.Update(s =>
                {
                    s.DeviceConnected = false;
                    s.DeviceStatus = error ?? "zkemkeeper not registered";
                    s.LastError = error;
                    s.SiteId = _options.SiteId;
                });
                _started.Set();
                return;
            }

            _started.Set();
            var pollMs = Math.Max(3, _options.Device.PollIntervalSeconds) * 1000;

            while (_shouldRun)
            {
                try
                {
                    _wake.WaitOne(pollMs);
                    if (!_shouldRun) break;

                    if (!_connected)
                    {
                        if (TryConnect())
                        {
                            _serialNumber = ReadSerialNumber();
                            _status.Update(s =>
                            {
                                s.DeviceConnected = true;
                                s.DeviceSerialNumber = _serialNumber;
                                s.DeviceStatus = "Connected";
                                s.SiteId = _options.SiteId;
                            });
                            _logger.LogInformation("Connected to device {Ip} SN={Sn}", _options.Device.Ip, _serialNumber);
                        }
                        else
                        {
                            _status.Update(s =>
                            {
                                s.DeviceConnected = false;
                                s.DeviceStatus = "Unreachable";
                                s.SiteId = _options.SiteId;
                            });
                        }
                    }
                    else
                    {
                        PollLogs();
                    }
                }
                catch (COMException ex)
                {
                    _logger.LogWarning(ex, "COM error talking to device; will reconnect");
                    MarkDisconnected("COM error");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected device loop error");
                    _status.Update(s => s.LastError = ex.Message);
                    MarkDisconnected("Error");
                }
            }
        }
        finally
        {
            _zk?.Dispose();
            _zk = null;
        }
    }

    private bool TryConnect()
    {
        if (_zk is null) return false;

        try { _zk.Disconnect(); } catch { /* ignore */ }

        try
        {
            if (_zk.SetCommPassword(_options.Device.CommKey) &&
                _zk.ConnectNet(_options.Device.Ip, _options.Device.Port))
            {
                _zk.EnableDevice(_options.Device.MachineNumber, true);
                _connected = true;
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Connect attempt failed");
        }

        _connected = false;
        return false;
    }

    private void PollLogs()
    {
        if (_zk is null) return;

        if (!_zk.ReadGeneralLogData(_options.Device.MachineNumber))
        {
            var err = 0;
            _zk.GetLastError(ref err);
            if (err != 0)
            {
                _logger.LogWarning("ReadGeneralLogData failed, lastError={Error}", err);
                MarkDisconnected("Read failed");
            }
            return;
        }

        var newLogs = new List<LocalPunch>();
        var maxSeen = _watermark;
        var workCode = 0;

        while (_zk.TryReadNextLog(
                   _options.Device.MachineNumber,
                   out var enrollNumber,
                   out var verifyMode,
                   out var inOutMode,
                   out var year,
                   out var month,
                   out var day,
                   out var hour,
                   out var minute,
                   out var second,
                   ref workCode))
        {
            DateTime timestamp;
            try
            {
                timestamp = new DateTime(year, month, day, hour, minute, second);
            }
            catch
            {
                continue;
            }

            if (timestamp > maxSeen)
                maxSeen = timestamp;

            if (timestamp <= _watermark)
                continue;

            newLogs.Add(new LocalPunch
            {
                EnrollNumber = enrollNumber,
                ModType = verifyMode,
                InOutMode = inOutMode,
                Timestamp = timestamp,
                WorkCode = workCode,
                DeviceSerialNumber = _serialNumber,
                SyncState = PunchSyncState.Pending
            });
        }

        if (newLogs.Count > 0)
        {
            var inserted = _store.SaveNewPunches(newLogs);
            _logger.LogInformation("Pulled {Pulled} new punches, inserted {Inserted}", newLogs.Count, inserted);
        }

        if (maxSeen > _watermark)
        {
            _watermark = maxSeen;
            _store.SetWatermark(_watermark);
        }

        _status.Update(s =>
        {
            s.LastDevicePollUtc = DateTime.UtcNow;
            s.DeviceConnected = true;
            s.DeviceStatus = "Connected";
            s.DeviceSerialNumber = _serialNumber;
        });
    }

    private string ReadSerialNumber()
    {
        try
        {
            if (_zk is not null && _zk.GetSerialNumber(_options.Device.MachineNumber, out var sn))
                return sn ?? "";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to read device serial");
        }

        return "";
    }

    private void MarkDisconnected(string reason)
    {
        _connected = false;
        try { _zk?.Disconnect(); } catch { /* ignore */ }
        _status.Update(s =>
        {
            s.DeviceConnected = false;
            s.DeviceStatus = reason;
        });
    }

    public void Dispose()
    {
        _shouldRun = false;
        _wake.Set();
        _wake.Dispose();
        _started.Dispose();
    }
}
