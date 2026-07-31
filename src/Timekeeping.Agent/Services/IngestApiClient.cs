using System.Net.Http.Json;
using System.Net.Sockets;
using Microsoft.Extensions.Options;
using Timekeeping.Agent.Configuration;
using Timekeeping.Contracts;

namespace Timekeeping.Agent.Services;

public sealed class IngestApiClient
{
    private readonly HttpClient _http;
    private readonly AgentOptions _options;
    private readonly ILogger<IngestApiClient> _logger;

    public IngestApiClient(HttpClient http, IOptions<AgentOptions> options, ILogger<IngestApiClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
        _http.BaseAddress = new Uri(_options.Ingest.BaseUrl.TrimEnd('/') + "/");
        _http.Timeout = TimeSpan.FromSeconds(Math.Max(5, _options.Ingest.TimeoutSeconds));
        _http.DefaultRequestHeaders.Remove(ApiKeyHeader);
        if (!string.IsNullOrWhiteSpace(_options.Ingest.ApiKey))
            _http.DefaultRequestHeaders.Add(ApiKeyHeader, _options.Ingest.ApiKey);
    }

    private const string ApiKeyHeader = "X-Api-Key";

    public async Task<bool> IsReachableAsync(CancellationToken ct)
    {
        try
        {
            using var response = await _http.GetAsync("health", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (IsTransient(ex))
        {
            _logger.LogDebug(ex, "Ingest API unreachable (VPN/network)");
            return false;
        }
    }

    public async Task<DateTime?> GetServerTimeAsync(CancellationToken ct)
    {
        try
        {
            var payload = await _http.GetFromJsonAsync<ServerTimeResponse>("api/time", ct);
            return payload?.ServerTime;
        }
        catch (Exception ex) when (IsTransient(ex))
        {
            _logger.LogDebug(ex, "Failed to get server time");
            return null;
        }
    }

    public async Task<PunchBatchResponse?> SendBatchAsync(PunchBatchRequest request, CancellationToken ct)
    {
        try
        {
            using var response = await _http.PostAsJsonAsync("api/punches/batch", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Ingest rejected batch: {Status} {Body}", response.StatusCode, body);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<PunchBatchResponse>(cancellationToken: ct);
        }
        catch (Exception ex) when (IsTransient(ex))
        {
            _logger.LogWarning(ex, "Batch upload failed due to network/VPN");
            return null;
        }
    }

    public static bool IsTransient(Exception ex)
        => ex is HttpRequestException
            or TaskCanceledException
            or IOException
            or SocketException
            || ex.InnerException is SocketException;
}
