using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Timekeeping.Contracts;
using Microsoft.AspNetCore.Http;


namespace Timekeeping.IngestApi.Services;

public sealed class HrisPunchRepository
{
    private readonly string _connectionString;
    private readonly ILogger<HrisPunchRepository> _logger;

    public HrisPunchRepository(IConfiguration configuration, ILogger<HrisPunchRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("Hris")
            ?? throw new InvalidOperationException("Connection string 'Hris' is required.");
        _logger = logger;
    }

    public async Task<bool> CanConnectAsync(CancellationToken ct)
    {
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "HRIS database health check failed");
            return false;
        }
    }

    public async Task<DateTime> GetServerTimeAsync(CancellationToken ct)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand("SELECT GETDATE()", conn);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is DateTime dt ? dt : DateTime.Now;
    }

    public async Task<PunchBatchResponse> IngestBatchAsync(PunchBatchRequest request, CancellationToken ct)
    {
        var response = new PunchBatchResponse { ServerTimeUtc = DateTime.UtcNow };

        if (request.Punches.Count == 0)
            return response;

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        var deviceSn = string.IsNullOrWhiteSpace(request.DeviceSerialNumber)
            ? request.Punches[0].DeviceSerialNumber
            : request.DeviceSerialNumber;

        var existing = await GetExistingKeysAsync(conn, request.Punches, deviceSn, ct);

        foreach (var punch in request.Punches)
        {
            var sn = string.IsNullOrWhiteSpace(punch.DeviceSerialNumber) ? deviceSn : punch.DeviceSerialNumber;
            var key = BuildKey(punch.Timestamp, sn, punch.EnrollNumber, punch.InOutMode);

            if (existing.Contains(key))
            {
                response.Results.Add(new PunchIngestResult
                {
                    LocalId = punch.LocalId,
                    Status = PunchIngestStatus.Duplicate,
                    Message = "Already present in HRIS"
                });
                continue;
            }

            try
            {
                var mapped = await TryResolveEmployeeAsync(conn, punch.EnrollNumber, ct);
                if (mapped is null)
                {
                    response.Results.Add(new PunchIngestResult
                    {
                        LocalId = punch.LocalId,
                        Status = PunchIngestStatus.Unmapped,
                        Message = $"No HRIS mapping for enroll number {punch.EnrollNumber}"
                    });
                    continue;
                }

                // Also dedupe by resolved employee identity (legacy rows may lack enroll on the log row).
                var legacyKey = $"{punch.Timestamp:yyyy-MM-dd HH:mm:ss}|{sn}|{mapped.Value.EmployeeId}|{punch.InOutMode}";
                if (existing.Contains(legacyKey))
                {
                    response.Results.Add(new PunchIngestResult
                    {
                        LocalId = punch.LocalId,
                        Status = PunchIngestStatus.Duplicate,
                        Message = "Already present in HRIS"
                    });
                    continue;
                }

                await InsertLogAsync(conn, punch, sn, mapped.Value.PersonnelId, mapped.Value.EmployeeId, ct);
                existing.Add(key);
                existing.Add(legacyKey);

                response.Results.Add(new PunchIngestResult
                {
                    LocalId = punch.LocalId,
                    Status = PunchIngestStatus.Accepted
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ingest punch LocalId={LocalId} Enroll={Enroll}",
                    punch.LocalId, punch.EnrollNumber);
                response.Results.Add(new PunchIngestResult
                {
                    LocalId = punch.LocalId,
                    Status = PunchIngestStatus.Error,
                    Message = ex.Message
                });
            }
        }

        return response;
    }

    private static string BuildKey(DateTime timestamp, string deviceSn, string enroll, int inOutMode)
        => $"{timestamp:yyyy-MM-dd HH:mm:ss}|{deviceSn}|{enroll}|{inOutMode}";

    private static async Task<HashSet<string>> GetExistingKeysAsync(
        SqlConnection conn,
        List<PunchDto> punches,
        string deviceSn,
        CancellationToken ct)
    {
        var existing = new HashSet<string>(StringComparer.Ordinal);
        const int batchSize = 100;

        for (var i = 0; i < punches.Count; i += batchSize)
        {
            var batch = punches.Skip(i).Take(batchSize).ToList();
            await using var cmd = conn.CreateCommand();
            cmd.CommandTimeout = 60;

            var conditions = new List<string>(batch.Count);
            for (var j = 0; j < batch.Count; j++)
            {
                conditions.Add($"(logtime = @ts{j} AND device_sn = @sn{j})");
                var punchSn = string.IsNullOrWhiteSpace(batch[j].DeviceSerialNumber)
                    ? deviceSn
                    : batch[j].DeviceSerialNumber;
                cmd.Parameters.AddWithValue($"@ts{j}", batch[j].Timestamp);
                cmd.Parameters.AddWithValue($"@sn{j}", punchSn);
            }

            cmd.CommandText = $@"
                SELECT logtime, device_sn, action, user_id
                FROM [HRIS].[dbo].[timekeeping_LogRecord]
                WHERE {string.Join(" OR ", conditions)}";

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var timestamp = reader.GetDateTime(0);
                var sn = reader.IsDBNull(1) ? "" : reader.GetString(1);
                var action = reader.IsDBNull(2) ? 0 : Convert.ToInt32(reader.GetValue(2));
                var userId = reader.IsDBNull(3) ? "" : Convert.ToString(reader.GetValue(3)) ?? "";

                // Legacy-compatible key (employee-based) plus enroll-less device/time markers.
                existing.Add($"{timestamp:yyyy-MM-dd HH:mm:ss}|{sn}|{userId}|{action}");
                existing.Add($"{timestamp:yyyy-MM-dd HH:mm:ss}|{sn}");
            }
        }

        // Map enroll-based keys for punches whose device/time already exists.
        foreach (var punch in punches)
        {
            var sn = string.IsNullOrWhiteSpace(punch.DeviceSerialNumber) ? deviceSn : punch.DeviceSerialNumber;
            var deviceTimeKey = $"{punch.Timestamp:yyyy-MM-dd HH:mm:ss}|{sn}";
            if (existing.Contains(deviceTimeKey))
                existing.Add(BuildKey(punch.Timestamp, sn, punch.EnrollNumber, punch.InOutMode));
        }

        return existing;
    }

    private static async Task<(object PersonnelId, object EmployeeId)?> TryResolveEmployeeAsync(
        SqlConnection conn,
        string enrollNumber,
        CancellationToken ct)
    {
        await using var empCmd = new SqlCommand(
            "SELECT emp_id FROM [HRIS].[dbo].[timekeeping_JointID] WHERE biometric_id = @enroll",
            conn);
        empCmd.Parameters.AddWithValue("@enroll", enrollNumber);
        var personnelId = await empCmd.ExecuteScalarAsync(ct);
        if (personnelId is null or DBNull)
            return null;

        await using var userCmd = new SqlCommand(
            "SELECT employee_id FROM [HRIS].[dbo].[PMS_personnel_information] WHERE hris_id = @hris_id",
            conn);
        userCmd.Parameters.AddWithValue("@hris_id", personnelId);
        var employeeId = await userCmd.ExecuteScalarAsync(ct);
        if (employeeId is null or DBNull)
            return null;

        return (personnelId, employeeId);
    }

    private static async Task InsertLogAsync(
        SqlConnection conn,
        PunchDto punch,
        string deviceSn,
        object personnelId,
        object employeeId,
        CancellationToken ct)
    {
        await using var cmd = new SqlCommand(@"
            INSERT INTO [HRIS].[dbo].[timekeeping_LogRecord]
                ([personnel_id],[user_id],[modtype],[action],[logtime],
                 [workcode],[device_sn],[sched],[flexi],[no_mandosched])
            VALUES
                (@personnel_id, @user_id, @modtype, @action, @logtime,
                 @workcode, @device_sn, @sched, @flexi, @no_mandosched)", conn);

        cmd.Parameters.AddWithValue("@personnel_id", personnelId);
        cmd.Parameters.AddWithValue("@user_id", employeeId);
        cmd.Parameters.AddWithValue("@modtype", punch.ModType);
        cmd.Parameters.AddWithValue("@action", punch.InOutMode);
        cmd.Parameters.AddWithValue("@logtime", punch.Timestamp);
        cmd.Parameters.AddWithValue("@workcode", punch.WorkCode);
        cmd.Parameters.AddWithValue("@device_sn", deviceSn);
        cmd.Parameters.AddWithValue("@sched", DBNull.Value);
        cmd.Parameters.AddWithValue("@flexi", 0);
        cmd.Parameters.AddWithValue("@no_mandosched", 0);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}

public static class ApiKeyAuth
{
    public const string HeaderName = "X-Api-Key";

    public static bool IsValid(HttpContext context, IConfiguration configuration)
    {
        var expected = configuration["Ingest:ApiKey"];
        if (string.IsNullOrWhiteSpace(expected))
            return false;

        if (!context.Request.Headers.TryGetValue(HeaderName, out var provided) ||
            string.IsNullOrWhiteSpace(provided))
            return false;

        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided.ToString());
        return expectedBytes.Length == providedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}
