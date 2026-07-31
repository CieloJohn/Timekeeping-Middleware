using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Timekeeping.Agent.Configuration;
using Timekeeping.Agent.Storage;

namespace Timekeeping.Agent.Services;

/// <summary>
/// Loopback-only status endpoint for the tray UI. Not exposed over the VPN.
/// </summary>
public sealed class LocalStatusHost : BackgroundService
{
    private readonly AgentStatusHub _status;
    private readonly LocalPunchStore _store;
    private readonly AgentOptions _options;
    private readonly ILogger<LocalStatusHost> _logger;
    private HttpListener? _listener;

    public LocalStatusHost(
        AgentStatusHub status,
        LocalPunchStore store,
        IOptions<AgentOptions> options,
        ILogger<LocalStatusHost> logger)
    {
        _status = status;
        _store = store;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var prefix = _options.Status.ListenUrl.TrimEnd('/') + "/";
        if (!prefix.StartsWith("http://127.0.0.1", StringComparison.OrdinalIgnoreCase) &&
            !prefix.StartsWith("http://localhost", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError("Status ListenUrl must be loopback-only. Got {Url}", prefix);
            return;
        }

        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix.EndsWith("/") ? prefix : prefix + "/");

        try
        {
            _listener.Start();
            _logger.LogInformation("Local status host listening on {Url}", prefix);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start local status host on {Url}", prefix);
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            HttpListenerContext? context = null;
            try
            {
                context = await _listener.GetContextAsync().WaitAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Status host accept error");
                continue;
            }

            _ = Task.Run(() => Handle(context), CancellationToken.None);
        }

        try { _listener.Stop(); } catch { /* ignore */ }
    }

    private void Handle(HttpListenerContext context)
    {
        try
        {
            var path = context.Request.Url?.AbsolutePath.TrimEnd('/') ?? "";
            var method = context.Request.HttpMethod;

            if (method == "GET" &&
                (path.Equals("/status", StringComparison.OrdinalIgnoreCase) ||
                 path.Equals("", StringComparison.OrdinalIgnoreCase) ||
                 path.Equals("/", StringComparison.OrdinalIgnoreCase)))
            {
                WriteJson(context, 200, _status.Snapshot());
                return;
            }

            if (method == "POST" && path.Equals("/requeue-unmapped", StringComparison.OrdinalIgnoreCase))
            {
                var count = _store.RequeueUnmapped();
                WriteJson(context, 200, new { requeued = count });
                return;
            }

            context.Response.StatusCode = 404;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Status response failed");
            try { context.Response.StatusCode = 500; } catch { /* ignore */ }
        }
        finally
        {
            try { context.Response.OutputStream.Close(); } catch { /* ignore */ }
        }
    }

    private static void WriteJson(HttpListenerContext context, int statusCode, object payload)
    {
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        context.Response.OutputStream.Write(bytes, 0, bytes.Length);
    }

    public override void Dispose()
    {
        try { _listener?.Close(); } catch { /* ignore */ }
        base.Dispose();
    }
}
