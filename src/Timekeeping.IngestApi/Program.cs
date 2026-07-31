using Timekeeping.Contracts;
using Timekeeping.IngestApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<HrisPunchRepository>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", async (HrisPunchRepository repo, CancellationToken ct) =>
{
    var dbOk = await repo.CanConnectAsync(ct);
    return Results.Ok(new HealthResponse
    {
        Status = dbOk ? "ok" : "degraded",
        ServerTimeUtc = DateTime.UtcNow,
        DatabaseOk = dbOk
    });
});

app.MapGet("/api/time", async (HttpContext http, IConfiguration config, HrisPunchRepository repo, CancellationToken ct) =>
{
    if (!ApiKeyAuth.IsValid(http, config))
        return Results.Unauthorized();

    var serverTime = await repo.GetServerTimeAsync(ct);
    return Results.Ok(new ServerTimeResponse
    {
        ServerTime = serverTime,
        ServerTimeUtc = DateTime.UtcNow
    });
});

app.MapPost("/api/punches/batch", async (
    PunchBatchRequest request,
    HttpContext http,
    IConfiguration config,
    HrisPunchRepository repo,
    ILoggerFactory loggerFactory,
    CancellationToken ct) =>
{
    if (!ApiKeyAuth.IsValid(http, config))
        return Results.Unauthorized();

    if (request.Punches is null || request.Punches.Count == 0)
        return Results.BadRequest("Punches collection is required.");

    var maxBatch = config.GetValue("Ingest:MaxBatchSize", 200);
    if (request.Punches.Count > maxBatch)
        return Results.BadRequest($"Batch size exceeds limit of {maxBatch}.");

    var logger = loggerFactory.CreateLogger("PunchIngest");
    logger.LogInformation(
        "Ingesting {Count} punches from site {SiteId} device {DeviceSn}",
        request.Punches.Count,
        request.SiteId,
        request.DeviceSerialNumber);

    var response = await repo.IngestBatchAsync(request, ct);
    return Results.Ok(response);
});

app.Run();
