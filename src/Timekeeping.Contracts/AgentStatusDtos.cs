namespace Timekeeping.Contracts;

public sealed class AgentStatusDto
{
    public string SiteId { get; set; } = "";
    public string DeviceSerialNumber { get; set; } = "";
    public bool DeviceConnected { get; set; }
    public bool VpnOrApiReachable { get; set; }
    public string DeviceStatus { get; set; } = "Unknown";
    public string SyncStatus { get; set; } = "Idle";
    public int PendingCount { get; set; }
    public int FailedCount { get; set; }
    public int UnmappedCount { get; set; }
    public DateTime? LastSuccessfulSyncUtc { get; set; }
    public DateTime? LastDevicePollUtc { get; set; }
    public string? LastError { get; set; }
    public DateTime ReportedAtUtc { get; set; }
}
