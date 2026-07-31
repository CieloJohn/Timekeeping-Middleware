using Microsoft.Data.Sqlite;

namespace Timekeeping.Agent.Storage;

public sealed class LocalPunchStore
{
    private readonly string _dbPath;
    private readonly object _initLock = new();
    private bool _initialized;

    public LocalPunchStore()
    {
        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "TimekeepingAgent");
        Directory.CreateDirectory(dataDir);
        _dbPath = Path.Combine(dataDir, "punches.db");
    }

    private string ConnectionString => $"Data Source={_dbPath}";

    public void EnsureCreated()
    {
        if (_initialized) return;
        lock (_initLock)
        {
            if (_initialized) return;

            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Punches (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EnrollNumber TEXT NOT NULL,
    InOutMode INTEGER NOT NULL,
    ModType INTEGER NOT NULL,
    Timestamp TEXT NOT NULL,
    WorkCode INTEGER NOT NULL DEFAULT 0,
    DeviceSerialNumber TEXT NOT NULL DEFAULT '',
    SyncState INTEGER NOT NULL DEFAULT 0,
    LastError TEXT NULL,
    AttemptCount INTEGER NOT NULL DEFAULT 0,
    CreatedAtUtc TEXT NOT NULL,
    UpdatedAtUtc TEXT NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_Punches_Identity
    ON Punches(EnrollNumber, InOutMode, ModType, Timestamp, WorkCode, DeviceSerialNumber);
CREATE TABLE IF NOT EXISTS AgentMeta (
    Key TEXT PRIMARY KEY,
    Value TEXT NOT NULL
);";
                cmd.ExecuteNonQuery();
            }

            _initialized = true;
        }
    }

    public int SaveNewPunches(IEnumerable<LocalPunch> punches)
    {
        EnsureCreated();
        var inserted = 0;
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var tx = conn.BeginTransaction();

        foreach (var punch in punches)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
INSERT OR IGNORE INTO Punches
    (EnrollNumber, InOutMode, ModType, Timestamp, WorkCode, DeviceSerialNumber,
     SyncState, LastError, AttemptCount, CreatedAtUtc, UpdatedAtUtc)
VALUES
    (@enroll, @inout, @mod, @ts, @work, @sn, 0, NULL, 0, @now, @now);";
            cmd.Parameters.AddWithValue("@enroll", punch.EnrollNumber);
            cmd.Parameters.AddWithValue("@inout", punch.InOutMode);
            cmd.Parameters.AddWithValue("@mod", punch.ModType);
            cmd.Parameters.AddWithValue("@ts", punch.Timestamp.ToString("o"));
            cmd.Parameters.AddWithValue("@work", punch.WorkCode);
            cmd.Parameters.AddWithValue("@sn", punch.DeviceSerialNumber ?? "");
            cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));
            inserted += cmd.ExecuteNonQuery();
        }

        tx.Commit();
        return inserted;
    }

    public List<LocalPunch> GetRetryablePunches(int limit)
    {
        EnsureCreated();
        var result = new List<LocalPunch>();
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT Id, EnrollNumber, InOutMode, ModType, Timestamp, WorkCode, DeviceSerialNumber,
       SyncState, LastError, AttemptCount
FROM Punches
WHERE SyncState IN (0, 3)
ORDER BY Timestamp ASC, Id ASC
LIMIT @limit;";
        cmd.Parameters.AddWithValue("@limit", limit);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add(ReadPunch(reader));

        return result;
    }

    public void MarkResults(IEnumerable<(long Id, PunchSyncState State, string? Error)> results)
    {
        EnsureCreated();
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var tx = conn.BeginTransaction();

        foreach (var (id, state, error) in results)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
UPDATE Punches
SET SyncState = @state,
    LastError = @error,
    AttemptCount = AttemptCount + 1,
    UpdatedAtUtc = @now
WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@state", (int)state);
            cmd.Parameters.AddWithValue("@error", (object?)error ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
    }

    public int RequeueUnmapped()
    {
        EnsureCreated();
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
UPDATE Punches
SET SyncState = 0, LastError = NULL, UpdatedAtUtc = @now
WHERE SyncState = 2;";
        cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));
        return cmd.ExecuteNonQuery();
    }

    public (int Pending, int Failed, int Unmapped) GetCounts()
    {
        EnsureCreated();
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT
    SUM(CASE WHEN SyncState = 0 THEN 1 ELSE 0 END),
    SUM(CASE WHEN SyncState = 3 THEN 1 ELSE 0 END),
    SUM(CASE WHEN SyncState = 2 THEN 1 ELSE 0 END)
FROM Punches;";
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return (0, 0, 0);

        return (
            reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
            reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
            reader.IsDBNull(2) ? 0 : reader.GetInt32(2));
    }

    public DateTime? GetWatermark()
    {
        var value = GetMeta("LastPullWatermark");
        return DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
            ? dt
            : null;
    }

    public void SetWatermark(DateTime watermark)
    {
        SetMeta("LastPullWatermark", watermark.ToString("o"));
    }

    public string? GetMeta(string key)
    {
        EnsureCreated();
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Value FROM AgentMeta WHERE Key = @key";
        cmd.Parameters.AddWithValue("@key", key);
        return cmd.ExecuteScalar() as string;
    }

    public void SetMeta(string key, string value)
    {
        EnsureCreated();
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO AgentMeta(Key, Value) VALUES(@key, @value)
ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;";
        cmd.Parameters.AddWithValue("@key", key);
        cmd.Parameters.AddWithValue("@value", value);
        cmd.ExecuteNonQuery();
    }

    private static LocalPunch ReadPunch(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        EnrollNumber = reader.GetString(1),
        InOutMode = reader.GetInt32(2),
        ModType = reader.GetInt32(3),
        Timestamp = DateTime.Parse(reader.GetString(4), null, System.Globalization.DateTimeStyles.RoundtripKind),
        WorkCode = reader.GetInt32(5),
        DeviceSerialNumber = reader.GetString(6),
        SyncState = (PunchSyncState)reader.GetInt32(7),
        LastError = reader.IsDBNull(8) ? null : reader.GetString(8),
        AttemptCount = reader.GetInt32(9)
    };
}
