using Bud.Mcp.Auth;
using Bud.Mcp.Configuration;
using Bud.Mcp.Protocol;
using Bud.Mcp.Tools.Generation;

var builder = WebApplication.CreateBuilder(args);

var options = BudMcpOptions.FromConfiguration(builder.Configuration);

var commandRunner = new ToolCatalogCommandRunner();
var commandResult = await commandRunner.TryExecuteAsync(args, options);
if (commandResult.Handled)
{
    Environment.ExitCode = commandResult.ExitCode;
    return;
}

builder.Services.AddSingleton(options);
builder.Services.AddSingleton<IMcpSessionStore, InMemoryMcpSessionStore>();
builder.Services.AddSingleton<McpJsonRpcDispatcher>();
builder.Services.AddScoped<IMcpRequestProcessor, McpRequestProcessor>();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ok" }));

app.MapPost("/", (HttpContext httpContext, IMcpRequestProcessor requestProcessor, CancellationToken cancellationToken)
    => requestProcessor.ProcessAsync(httpContext, cancellationToken));

await app.RunAsync();

public partial class Program;
