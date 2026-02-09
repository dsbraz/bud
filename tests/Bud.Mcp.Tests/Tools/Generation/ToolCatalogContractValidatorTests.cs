using System.Text.Json.Nodes;
using Bud.Mcp.Tools;
using Bud.Mcp.Tools.Generation;

namespace Bud.Mcp.Tests.Tools.Generation;

public sealed class ToolCatalogContractValidatorTests
{
    [Fact]
    public void ValidateRequiredFields_WhenCatalogIsValid_ReturnsNoErrors()
    {
        var errors = ToolCatalogContractValidator.ValidateRequiredFields(CreateValidTools());
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateRequiredFields_WhenRequiredFieldIsMissing_ReturnsError()
    {
        var tools = CreateValidTools().ToList();
        var missionCreate = tools.Single(tool => tool.Name == "mission_create");
        missionCreate.InputSchema["required"] = new JsonArray("name");

        var errors = ToolCatalogContractValidator.ValidateRequiredFields(tools);

        errors.Should().Contain(error => error.Contains("mission_create", StringComparison.Ordinal));
        errors.Should().Contain(error => error.Contains("startDate", StringComparison.Ordinal));
    }

    private static IReadOnlyList<McpToolDefinition> CreateValidTools()
    {
        return
        [
            CreateTool("mission_create", ["name", "startDate", "endDate", "status", "scopeType", "scopeId"]),
            CreateTool("mission_get", ["id"]),
            CreateTool("mission_list", []),
            CreateTool("mission_update", ["id", "payload"]),
            CreateTool("mission_delete", ["id"]),
            CreateTool("mission_metric_create", ["missionId", "name", "type"]),
            CreateTool("mission_metric_get", ["id"]),
            CreateTool("mission_metric_list", []),
            CreateTool("mission_metric_update", ["id", "payload"]),
            CreateTool("mission_metric_delete", ["id"]),
            CreateTool("metric_checkin_create", ["missionMetricId", "checkinDate", "confidenceLevel"]),
            CreateTool("metric_checkin_get", ["id"]),
            CreateTool("metric_checkin_list", []),
            CreateTool("metric_checkin_update", ["id", "payload"]),
            CreateTool("metric_checkin_delete", ["id"])
        ];
    }

    private static McpToolDefinition CreateTool(string name, IEnumerable<string> requiredFields)
    {
        var required = new JsonArray(requiredFields.Select(field => (JsonNode?)field).ToArray());
        return new McpToolDefinition(
            name,
            name,
            new JsonObject
            {
                ["type"] = "object",
                ["required"] = required,
                ["properties"] = new JsonObject()
            });
    }
}
