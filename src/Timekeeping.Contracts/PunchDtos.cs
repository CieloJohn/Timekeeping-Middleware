namespace Timekeeping.Contracts;

public sealed class PunchDto
{
    public long? LocalId { get; set; }
    public string EnrollNumber { get; set; } = "";
    public int InOutMode { get; set; }
    public int ModType { get; set; }
    public DateTime Timestamp { get; set; }
    public int WorkCode { get; set; }
    public string DeviceSerialNumber { get; set; } = "";
}

public sealed class PunchBatchRequest
{
    public string SiteId { get; set; } = "";
    public string DeviceSerialNumber { get; set; } = "";
    public List<PunchDto> Punches { get; set; } = new();
}

public enum PunchIngestStatus
{
    Accepted = 0,
    Duplicate = 1,
    Unmapped = 2,
    Error = 3
}

public sealed class PunchIngestResult
{
    public long? LocalId { get; set; }
    public PunchIngestStatus Status { get; set; }
    public string? Message { get; set; }
}

public sealed class PunchBatchResponse
{
    public List<PunchIngestResult> Results { get; set; } = new();
    public DateTime ServerTimeUtc { get; set; }
}

public sealed class HealthResponse
{
    public string Status { get; set; } = "ok";
    public DateTime ServerTimeUtc { get; set; }
    public bool DatabaseOk { get; set; }
}

public sealed class ServerTimeResponse
{
    public DateTime ServerTime { get; set; }
    public DateTime ServerTimeUtc { get; set; }
}
