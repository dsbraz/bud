using Bud.Mcp.Tools;
using System.Text.Json.Nodes;

namespace Bud.Mcp.Tools.Generation;

public static class ToolCatalogContractValidator
{
    private static readonly Dictionary<string, string[]> RequiredByTool = new(StringComparer.Ordinal)
    {
        ["mission_create"] = ["name", "startDate", "endDate", "status", "scopeType", "scopeId"],
        ["mission_metric_create"] = ["missionId", "name", "type"],
        ["metric_checkin_create"] = ["missionMetricId", "checkinDate", "confidenceLevel"],
        ["mission_update"] = ["id", "payload"],
        ["mission_metric_update"] = ["id", "payload"],
        ["metric_checkin_update"] = ["id", "payload"],
        ["mission_get"] = ["id"],
        ["mission_delete"] = ["id"],
        ["mission_metric_get"] = ["id"],
        ["mission_metric_delete"] = ["id"],
        ["metric_checkin_get"] = ["id"],
        ["metric_checkin_delete"] = ["id"]
    };

    public static IReadOnlyList<string> ValidateRequiredFields(IReadOnlyList<McpToolDefinition> tools)
    {
        var errors = new List<string>();
        var byName = tools.ToDictionary(tool => tool.Name, tool => tool, StringComparer.Ordinal);

        foreach (var (toolName, requiredProperties) in RequiredByTool)
        {
            if (!byName.TryGetValue(toolName, out var tool))
            {
                errors.Add($"Tool obrigatória ausente no catálogo: {toolName}.");
                continue;
            }

            var requiredInSchema = GetRequiredSet(tool.InputSchema);
            foreach (var requiredProperty in requiredProperties)
            {
                if (!requiredInSchema.Contains(requiredProperty))
                {
                    errors.Add($"Tool '{toolName}' sem campo obrigatório '{requiredProperty}' no schema.");
                }
            }
        }

        return errors;
    }

    private static HashSet<string> GetRequiredSet(JsonObject schema)
    {
        if (schema["required"] is not JsonArray requiredArray)
        {
            return [];
        }

        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in requiredArray)
        {
            var value = entry?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(value))
            {
                set.Add(value);
            }
        }

        return set;
    }
}
