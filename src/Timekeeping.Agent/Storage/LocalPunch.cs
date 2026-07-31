namespace Timekeeping.Agent.Storage;

public enum PunchSyncState
{
    Pending = 0,
    Synced = 1,
    Unmapped = 2,
    Failed = 3
}

public sealed class LocalPunch
{
    public long Id { get; set; }
    public string EnrollNumber { get; set; } = "";
    public int InOutMode { get; set; }
    public int ModType { get; set; }
    public DateTime Timestamp { get; set; }
    public int WorkCode { get; set; }
    public string DeviceSerialNumber { get; set; } = "";
    public PunchSyncState SyncState { get; set; }
    public string? LastError { get; set; }
    public int AttemptCount { get; set; }
}
