using Bud.Mcp.Auth;
using Bud.Mcp.Configuration;
using Bud.Mcp.Observability;
using Bud.Mcp.Protocol;
using Bud.Mcp.Tools.Generation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var options = BudMcpOptions.FromConfiguration(builder.Configuration);

var commandRunner = new ToolCatalogCommandRunner();
var commandResult = await commandRunner.TryExecuteAsync(args, options);
if (commandResult.Handled)
{
    Environment.ExitCode = commandResult.ExitCode;
    return;
}

// Structured JSON logging for Cloud Logging (non-development environments)
if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
{
    builder.Logging.ClearProviders();
    builder.Services.AddLogging(lb =>
    {
        lb.AddConsoleFormatter<CloudLoggingJsonFormatter, CloudLoggingJsonFormatterOptions>(o =>
        {
            o.GcpProjectId = builder.Configuration["GCP_PROJECT_ID"];
            o.IncludeScopes = true;
        });
        lb.AddConsole(o => o.FormatterName = CloudLoggingJsonFormatter.FormatterName);
    });
}

// OpenTelemetry — instrumentation only; export config comes from OTel env vars
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(o =>
        {
            o.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
        })
        .AddHttpClientInstrumentation())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation())
    .UseOtlpExporter();

builder.Services.AddSingleton(options);
builder.Services.AddSingleton<IMcpSessionStore, InMemoryMcpSessionStore>();
builder.Services.AddSingleton<McpJsonRpcDispatcher>();
builder.Services.AddScoped<IMcpRequestProcessor, McpRequestProcessor>();

// Health checks: liveness always passes; readiness checks connectivity to Bud.Server
builder.Services.AddHealthChecks()
    .AddUrlGroup(
        new Uri(new Uri(options.ApiBaseUrl), "/health/ready"),
        name: "bud-server-api",
        tags: ["ready"]);

var app = builder.Build();

app.UseMiddleware<McpRequestLoggingMiddleware>();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = hc => hc.Tags.Contains("ready")
});

app.MapPost("/", (HttpContext httpContext, IMcpRequestProcessor requestProcessor, CancellationToken cancellationToken)
    => requestProcessor.ProcessAsync(httpContext, cancellationToken));

await app.RunAsync();

public partial class Program;
