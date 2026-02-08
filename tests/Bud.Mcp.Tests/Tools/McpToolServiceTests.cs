using Bud.Mcp.Tests.Helpers;
using Bud.Mcp.Tools;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bud.Mcp.Tests.Tools;

public sealed class McpToolServiceTests
{
    [Fact]
    public void GetTools_MissionCreateSchema_ExposesRequiredFieldsAndEnums()
    {
        var service = CreateService();

        var tool = service.GetTools().Single(t => t.Name == "mission_create");
        var required = tool.InputSchema["required"]!.AsArray().Select(n => n!.GetValue<string>()).ToHashSet();

        required.Should().Contain(new[] { "name", "startDate", "endDate", "status", "scopeType", "scopeId" });
        tool.InputSchema["additionalProperties"]!.GetValue<bool>().Should().BeFalse();

        var properties = tool.InputSchema["properties"]!.AsObject();
        properties["scopeId"]!["format"]!.GetValue<string>().Should().Be("uuid");
        properties["startDate"]!["format"]!.GetValue<string>().Should().Be("date-time");

        var statusEnum = properties["status"]!["enum"]!.AsArray().Select(n => n!.GetValue<string>());
        statusEnum.Should().Contain(new[] { "Planned", "Active", "Completed", "Cancelled" });
    }

    [Fact]
    public async Task ExecuteAsync_MissionCreateWithoutRequiredFields_ThrowsClearValidationMessage()
    {
        var service = CreateService();
        using var doc = JsonDocument.Parse("""
        {
          "name": "Missão sem campos"
        }
        """);

        var act = () => service.ExecuteAsync("mission_create", doc.RootElement);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Parâmetro obrigatório ausente: startDate.");
    }

    [Fact]
    public void GetTools_MissionMetricAndCheckinSchemas_ExposeRequiredFields()
    {
        var service = CreateService();

        var metricCreate = service.GetTools().Single(t => t.Name == "mission_metric_create");
        var metricRequired = metricCreate.InputSchema["required"]!.AsArray().Select(n => n!.GetValue<string>()).ToHashSet();
        metricRequired.Should().Contain(new[] { "missionId", "name", "type" });

        var checkinCreate = service.GetTools().Single(t => t.Name == "metric_checkin_create");
        var checkinRequired = checkinCreate.InputSchema["required"]!.AsArray().Select(n => n!.GetValue<string>()).ToHashSet();
        checkinRequired.Should().Contain(new[] { "missionMetricId", "checkinDate", "confidenceLevel" });

        service.GetTools().Select(t => t.Name).Should().Contain("session_bootstrap");
    }

    [Fact]
    public async Task ExecuteAsync_HelpActionSchemaWithoutAction_ReturnsAllActions()
    {
        var service = CreateService();
        using var doc = JsonDocument.Parse("{}");

        var result = await service.ExecuteAsync("help_action_schema", doc.RootElement);

        var actions = result["actions"]!.AsArray();
        actions.Should().NotBeEmpty();
        actions.Select(item => item!["name"]!.GetValue<string>()).Should().Contain("mission_create");
    }

    [Fact]
    public async Task ExecuteAsync_HelpActionSchemaWithAction_ReturnsSchemaAndExample()
    {
        var service = CreateService();
        using var doc = JsonDocument.Parse("""
        {
          "action": "mission_create"
        }
        """);

        var result = await service.ExecuteAsync("help_action_schema", doc.RootElement);

        result["name"]!.GetValue<string>().Should().Be("mission_create");
        result["required"]!.AsArray().Select(i => i!.GetValue<string>())
            .Should().Contain(new[] { "name", "startDate", "endDate", "status", "scopeType", "scopeId" });
        result["example"]!["scopeType"]!.GetValue<string>().Should().Be("Organization");
    }

    [Fact]
    public async Task ExecuteAsync_AuthLogin_ReturnsBootstrapHints()
    {
        var service = CreateAuthenticatedService();
        using var doc = JsonDocument.Parse("""
        {
          "email": "user@getbud.co"
        }
        """);

        var result = await service.ExecuteAsync("auth_login", doc.RootElement);

        result["requiresTenantForDomainTools"]!.GetValue<bool>().Should().BeTrue();
        result["nextSteps"]!.AsArray().Select(i => i!.GetValue<string>())
            .Should().Contain(new[] { "tenant_list_available", "tenant_set_current", "help_action_schema", "session_bootstrap" });
        result["whoami"]!["email"]!.GetValue<string>().Should().Be("user@getbud.co");
    }

    [Fact]
    public async Task ExecuteAsync_SessionBootstrap_ReturnsTenantsAndStarterSchemas()
    {
        var service = CreateAuthenticatedService();
        using var empty = JsonDocument.Parse("{}");
        using var loginDoc = JsonDocument.Parse("""{"email":"user@getbud.co"}""");
        await service.ExecuteAsync("auth_login", loginDoc.RootElement);

        var result = await service.ExecuteAsync("session_bootstrap", empty.RootElement);

        result["availableTenants"]!.AsArray().Should().NotBeEmpty();
        result["starterSchemas"]!.AsArray()
            .Select(item => item!["name"]!.GetValue<string>())
            .Should().Contain(new[] { "mission_create", "mission_metric_create", "metric_checkin_create" });
    }

    private static McpToolService CreateService()
    {
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("Request inesperada."));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://bud.test") };
        var options = new BudMcpOptions("http://bud.test", null, null, 30);
        var session = new BudApiSession(httpClient, options);
        var client = new BudApiClient(httpClient, session);
        return new McpToolService(client, session);
    }

    private static McpToolService CreateAuthenticatedService()
    {
        var tenantId = Guid.NewGuid();
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/auth/login")
            {
                return JsonResponse(new AuthLoginResponse
                {
                    Token = "jwt-token",
                    Email = "user@getbud.co",
                    DisplayName = "Usuário"
                });
            }

            if (request.RequestUri.AbsolutePath == "/api/auth/my-organizations")
            {
                return JsonResponse(new List<OrganizationSummaryDto>
                {
                    new() { Id = tenantId, Name = "Org 1" }
                });
            }

            throw new InvalidOperationException("Request inesperada.");
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://bud.test") };
        var options = new BudMcpOptions("http://bud.test", null, null, 30);
        var session = new BudApiSession(httpClient, options);
        var client = new BudApiClient(httpClient, session);
        return new McpToolService(client, session);
    }

    private static HttpResponseMessage JsonResponse<T>(T payload)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload))
        };
    }
}
