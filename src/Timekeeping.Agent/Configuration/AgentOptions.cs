namespace Timekeeping.Agent.Configuration;

public sealed class AgentOptions
{
    public const string SectionName = "Agent";

    public string SiteId { get; set; } = "SITE-01";
    public DeviceOptions Device { get; set; } = new();
    public IngestOptions Ingest { get; set; } = new();
    public SyncOptions Sync { get; set; } = new();
    public StatusOptions Status { get; set; } = new();
}

public sealed class DeviceOptions
{
    public string Ip { get; set; } = "192.168.1.201";
    public int Port { get; set; } = 4370;
    public int CommKey { get; set; }
    public int MachineNumber { get; set; } = 1;
    public int PollIntervalSeconds { get; set; } = 10;
}

public sealed class IngestOptions
{
    public string BaseUrl { get; set; } = "http://127.0.0.1:5080";
    public string ApiKey { get; set; } = "";
    public int BatchSize { get; set; } = 50;
    public int TimeoutSeconds { get; set; } = 30;
}

public sealed class SyncOptions
{
    public int SyncIntervalSeconds { get; set; } = 30;
    public int InitialBackoffSeconds { get; set; } = 5;
    public int MaxBackoffSeconds { get; set; } = 300;
    public bool SyncDeviceClockDaily { get; set; } = true;
    public string DeviceClockSyncTimeLocal { get; set; } = "02:00";
}

public sealed class StatusOptions
{
    public string ListenUrl { get; set; } = "http://127.0.0.1:17890";
}
