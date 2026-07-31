using Timekeeping.Agent.Configuration;
using Timekeeping.Agent.Services;
using Timekeeping.Agent.Storage;
using Timekeeping.Agent.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "PVAO Timekeeping Agent";
});

builder.Services
    .AddOptions<AgentOptions>()
    .Bind(builder.Configuration.GetSection(AgentOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.Device.Ip), "Agent:Device:Ip is required")
    .Validate(o => !string.IsNullOrWhiteSpace(o.Ingest.BaseUrl), "Agent:Ingest:BaseUrl is required")
    .Validate(o => !string.IsNullOrWhiteSpace(o.Ingest.ApiKey), "Agent:Ingest:ApiKey is required")
    .ValidateOnStart();

builder.Services.AddSingleton<LocalPunchStore>();
builder.Services.AddSingleton<AgentStatusHub>();
builder.Services.AddSingleton<ZkDeviceService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ZkDeviceService>());
builder.Services.AddHttpClient<IngestApiClient>();
builder.Services.AddHostedService<SyncWorker>();
builder.Services.AddHostedService<LocalStatusHost>();

var host = builder.Build();
host.Run();
