using Bud.Mcp.Configuration;

namespace Bud.Mcp.Tools.Generation;

public sealed class ToolCatalogCommandRunner
{
    private readonly Func<BudMcpOptions, HttpClient> _httpClientFactory;

    public ToolCatalogCommandRunner()
        : this(CreateHttpClient)
    {
    }

    public ToolCatalogCommandRunner(Func<BudMcpOptions, HttpClient> httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public async Task<ToolCatalogCommandResult> TryExecuteAsync(
        string[] args,
        BudMcpOptions options,
        CancellationToken cancellationToken = default)
    {
        if (args.Length == 0)
        {
            return ToolCatalogCommandResult.NotHandled;
        }

        var command = args[0];
        if (!string.Equals(command, "generate-tool-catalog", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(command, "check-tool-catalog", StringComparison.OrdinalIgnoreCase))
        {
            return ToolCatalogCommandResult.NotHandled;
        }

        using var httpClient = _httpClientFactory(options);

        return string.Equals(command, "generate-tool-catalog", StringComparison.OrdinalIgnoreCase)
            ? await GenerateToolCatalogAsyncCore(httpClient, cancellationToken)
            : await CheckToolCatalogAsyncCore(httpClient, args, cancellationToken);
    }

    private static async Task<ToolCatalogCommandResult> GenerateToolCatalogAsyncCore(HttpClient httpClient, CancellationToken cancellationToken)
    {
        var openApiJson = await httpClient.GetStringAsync("/openapi/v1.json", cancellationToken);
        var generated = OpenApiToolCatalogGenerator.BuildCatalogJson(openApiJson);
        await McpToolCatalogStore.WriteAsync(generated, cancellationToken);
        Console.Error.WriteLine("Catálogo MCP gerado com sucesso.");
        return ToolCatalogCommandResult.HandledSuccess;
    }

    private static async Task<ToolCatalogCommandResult> CheckToolCatalogAsyncCore(HttpClient httpClient, string[] args, CancellationToken cancellationToken)
    {
        var openApiJson = await httpClient.GetStringAsync("/openapi/v1.json", cancellationToken);
        var expectedRaw = OpenApiToolCatalogGenerator.BuildCatalogJson(openApiJson);
        var expected = McpToolCatalogStore.NormalizeJson(expectedRaw);
        var currentRaw = await McpToolCatalogStore.TryReadRawAsync(cancellationToken);
        var current = currentRaw is null ? string.Empty : McpToolCatalogStore.NormalizeJson(currentRaw);

        var expectedTools = McpToolCatalogStore.ParseToolsFromCatalogJson(expectedRaw);
        var requiredValidationErrors = ToolCatalogContractValidator.ValidateRequiredFields(expectedTools);
        if (requiredValidationErrors.Count > 0)
        {
            Console.Error.WriteLine("Catálogo MCP inválido: campos obrigatórios mínimos ausentes.");
            foreach (var error in requiredValidationErrors)
            {
                Console.Error.WriteLine($"- {error}");
            }

            return ToolCatalogCommandResult.HandledFailure;
        }

        var hasDiff = !string.Equals(expected, current, StringComparison.Ordinal);
        if (hasDiff)
        {
            Console.Error.WriteLine("Catálogo MCP está desatualizado. Execute generate-tool-catalog e commit o arquivo gerado.");
            var failOnDiff = args.Any(arg => string.Equals(arg, "--fail-on-diff", StringComparison.OrdinalIgnoreCase));
            return failOnDiff
                ? ToolCatalogCommandResult.HandledFailure
                : ToolCatalogCommandResult.HandledSuccess;
        }

        Console.Error.WriteLine("Catálogo MCP está atualizado.");
        return ToolCatalogCommandResult.HandledSuccess;
    }

    private static HttpClient CreateHttpClient(BudMcpOptions options)
    {
        return new HttpClient
        {
            BaseAddress = new Uri(options.ApiBaseUrl, UriKind.Absolute),
            Timeout = TimeSpan.FromSeconds(options.HttpTimeoutSeconds)
        };
    }
}

public readonly record struct ToolCatalogCommandResult(bool Handled, int ExitCode)
{
    public static ToolCatalogCommandResult NotHandled => new(false, 0);
    public static ToolCatalogCommandResult HandledSuccess => new(true, 0);
    public static ToolCatalogCommandResult HandledFailure => new(true, 1);
}
