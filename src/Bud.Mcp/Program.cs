using Bud.Mcp.Auth;
using Bud.Mcp.Configuration;
using Bud.Mcp.Http;
using Bud.Mcp.Protocol;
using Bud.Mcp.Tools;
using Bud.Mcp.Tools.Generation;
using Microsoft.Extensions.Configuration;

var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

var options = BudMcpOptions.FromConfiguration(configuration);

var commandRunner = new ToolCatalogCommandRunner();
var commandResult = await commandRunner.TryExecuteAsync(args, options);
if (commandResult.Handled)
{
    Environment.ExitCode = commandResult.ExitCode;
    return;
}

using var httpClient = new HttpClient
{
    BaseAddress = new Uri(options.ApiBaseUrl, UriKind.Absolute),
    Timeout = TimeSpan.FromSeconds(options.HttpTimeoutSeconds)
};

var session = new BudApiSession(httpClient, options);
await session.InitializeAsync();

var apiClient = new BudApiClient(httpClient, session);
var toolService = new McpToolService(apiClient, session);
var server = new StdioJsonRpcServer(toolService);

await server.RunAsync(Console.OpenStandardInput(), Console.OpenStandardOutput());
