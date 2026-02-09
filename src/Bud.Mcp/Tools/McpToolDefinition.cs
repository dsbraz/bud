using System.Text.Json.Nodes;

namespace Bud.Mcp.Tools;

public sealed record McpToolDefinition(string Name, string Description, JsonObject InputSchema);
