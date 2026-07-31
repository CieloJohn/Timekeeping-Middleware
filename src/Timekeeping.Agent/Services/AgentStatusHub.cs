using Timekeeping.Contracts;

namespace Timekeeping.Agent.Services;

public sealed class AgentStatusHub
{
    private readonly object _lock = new();
    private AgentStatusDto _status = new() { ReportedAtUtc = DateTime.UtcNow };

    public AgentStatusDto Snapshot()
    {
        lock (_lock)
        {
            return new AgentStatusDto
            {
                SiteId = _status.SiteId,
                DeviceSerialNumber = _status.DeviceSerialNumber,
                DeviceConnected = _status.DeviceConnected,
                VpnOrApiReachable = _status.VpnOrApiReachable,
                DeviceStatus = _status.DeviceStatus,
                SyncStatus = _status.SyncStatus,
                PendingCount = _status.PendingCount,
                FailedCount = _status.FailedCount,
                UnmappedCount = _status.UnmappedCount,
                LastSuccessfulSyncUtc = _status.LastSuccessfulSyncUtc,
                LastDevicePollUtc = _status.LastDevicePollUtc,
                LastError = _status.LastError,
                ReportedAtUtc = DateTime.UtcNow
            };
        }
    }

    public void Update(Action<AgentStatusDto> mutate)
    {
        lock (_lock)
        {
            mutate(_status);
            _status.ReportedAtUtc = DateTime.UtcNow;
        }
    }
}
