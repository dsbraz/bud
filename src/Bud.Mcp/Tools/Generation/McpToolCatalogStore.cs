using Bud.Mcp.Tools;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bud.Mcp.Tools.Generation;

public static class McpToolCatalogStore
{
    public const string CatalogRelativePath = "Tools/Generated/mcp-tool-catalog.json";

    public static string ResolveCatalogPath()
    {
        return Path.Combine(AppContext.BaseDirectory, CatalogRelativePath);
    }

    public static async Task WriteAsync(string json, CancellationToken cancellationToken = default)
    {
        var path = ResolveCatalogPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, json + Environment.NewLine, cancellationToken);
    }

    public static async Task<string?> TryReadRawAsync(CancellationToken cancellationToken = default)
    {
        var path = ResolveCatalogPath();
        if (!File.Exists(path))
        {
            return null;
        }

        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    public static IReadOnlyList<McpToolDefinition> LoadToolsOrEmpty()
    {
        var path = ResolveCatalogPath();
        if (!File.Exists(path))
        {
            return [];
        }

        var text = File.ReadAllText(path);
        return ParseToolsFromCatalogJson(text);
    }

    public static IReadOnlyList<McpToolDefinition> LoadToolsOrThrow()
    {
        var path = ResolveCatalogPath();
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Catálogo MCP não encontrado em '{path}'.");
        }

        var text = File.ReadAllText(path);
        var tools = ParseToolsFromCatalogJson(text);
        if (tools.Count == 0)
        {
            throw new InvalidOperationException($"Catálogo MCP inválido ou vazio em '{path}'.");
        }

        return tools;
    }

    public static IReadOnlyList<McpToolDefinition> ParseToolsFromCatalogJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        JsonObject? root;
        try
        {
            root = JsonNode.Parse(json)?.AsObject();
        }
        catch (JsonException)
        {
            return [];
        }

        if (root is null || root["tools"] is not JsonArray toolsArray)
        {
            return [];
        }

        var result = new List<McpToolDefinition>();
        foreach (var item in toolsArray)
        {
            if (item is not JsonObject toolObject)
            {
                continue;
            }

            var name = toolObject["name"]?.GetValue<string>();
            var description = toolObject["description"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description))
            {
                continue;
            }

            if (toolObject["inputSchema"] is not JsonObject schema)
            {
                continue;
            }

            result.Add(new McpToolDefinition(name, description, schema));
        }

        return result;
    }

    public static string NormalizeJson(string json)
    {
        var node = JsonNode.Parse(json) ?? throw new InvalidOperationException("JSON inválido para normalização.");
        return node.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }
}
