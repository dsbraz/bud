using System.Text.Json.Nodes;

namespace Bud.Mcp.Tools.Generation;

public static class ToolCatalogContractValidator
{
    private static readonly Dictionary<string, string[]> RequiredByTool = new(StringComparer.Ordinal)
    {
        ["goal_create"] = ["name", "startDate", "endDate", "status", "scopeType", "scopeId"],
        ["goal_indicator_create"] = ["goalId", "name", "type"],
        ["indicator_checkin_create"] = ["checkinDate", "confidenceLevel"],
        ["goal_update"] = ["id", "payload"],
        ["goal_indicator_update"] = ["id", "payload"],
        ["indicator_checkin_update"] = ["indicatorId", "checkinId", "payload"],
        ["goal_get"] = ["id"],
        ["goal_delete"] = ["id"],
        ["goal_indicator_get"] = ["id"],
        ["goal_indicator_delete"] = ["id"],
        ["indicator_checkin_get"] = ["indicatorId", "checkinId"],
        ["indicator_checkin_delete"] = ["indicatorId", "checkinId"]
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
