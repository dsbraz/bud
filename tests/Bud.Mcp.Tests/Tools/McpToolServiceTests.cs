using System.Text.Json;
using System.Text.Json.Nodes;
using System.Globalization;
using Bud.Mcp.Tests.Helpers;
using Bud.Mcp.Tools;
using Bud.Shared.Contracts;

namespace Bud.Mcp.Tests.Tools;

[Collection(Bud.Mcp.Tests.Tools.Generation.CatalogFileCollectionDefinition.Name)]
public sealed class McpToolServiceTests
{
    private static readonly string[] LoginNextSteps = ["tenant_list_available", "tenant_set_current", "help_list_actions", "help_action_schema", "session_bootstrap"];
    private static readonly string[] StarterSchemaNames = ["goal_create", "goal_indicator_create", "indicator_checkin_create"];

    [Fact]
    public void GetTools_MissionCreateSchema_ExposesKeyFields()
    {
        var service = CreateService();

        var tool = service.GetTools().Single(t => t.Name == "goal_create");
        var required = tool.InputSchema["required"]!.AsArray().Select(n => n!.GetValue<string>()).ToHashSet();

        required.Should().Contain("name");
        tool.InputSchema["additionalProperties"]!.GetValue<bool>().Should().BeFalse();

        var properties = tool.InputSchema["properties"]!.AsObject();
        properties.Should().ContainKeys("name", "startDate", "endDate", "status");
        properties["startDate"]!["format"]!.GetValue<string>().Should().Be("date-time");
        properties["status"]!["type"]!.GetValue<string>().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ExecuteAsync_MissionCreateWithoutRequiredFields_ThrowsClearValidationMessage()
    {
        var service = CreateService();
        using var doc = JsonDocument.Parse("""
        {}
        """);

        var act = () => service.ExecuteAsync("goal_create", doc.RootElement);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Parâmetro obrigatório ausente: name.");
    }

    [Fact]
    public void GetTools_MissionMetricAndCheckinSchemas_ExposeKeyFields()
    {
        var service = CreateService();

        var metricCreate = service.GetTools().Single(t => t.Name == "goal_indicator_create");
        var metricRequired = metricCreate.InputSchema["required"]!.AsArray().Select(n => n!.GetValue<string>()).ToHashSet();
        metricRequired.Should().Contain("goalId");
        metricCreate.InputSchema["properties"]!.AsObject().Should().ContainKeys("goalId", "name", "type");

        var checkinCreate = service.GetTools().Single(t => t.Name == "indicator_checkin_create");
        var checkinRequired = checkinCreate.InputSchema["required"]!.AsArray().Select(n => n!.GetValue<string>()).ToHashSet();
        checkinRequired.Should().Contain("checkinDate");
        checkinCreate.InputSchema["properties"]!.AsObject().Should().ContainKeys("checkinDate", "confidenceLevel");

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
        actions.Select(item => item!["name"]!.GetValue<string>()).Should().Contain("goal_create");
    }

    [Fact]
    public async Task ExecuteAsync_HelpListActions_ReturnsActionCatalogWithDescriptions()
    {
        var service = CreateService();
        using var doc = JsonDocument.Parse("{}");

        var result = await service.ExecuteAsync("help_list_actions", doc.RootElement);

        var actions = result["actions"]!.AsArray();
        actions.Should().NotBeEmpty();
        actions.Should().Contain(item => item!["name"]!.GetValue<string>() == "goal_create");
        actions.Should().Contain(item => item!["description"]!.GetValue<string>().Length > 0);
    }

    [Fact]
    public async Task ExecuteAsync_HelpActionSchemaWithAction_ReturnsSchemaAndExample()
    {
        var service = CreateService();
        using var doc = JsonDocument.Parse("""
        {
          "action": "goal_create"
        }
        """);

        var result = await service.ExecuteAsync("help_action_schema", doc.RootElement);

        result["name"]!.GetValue<string>().Should().Be("goal_create");
        result["required"]!.AsArray().Select(i => i!.GetValue<string>())
            .Should().Contain("name");
        result["example"]!["name"].Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_HelpActionSchemaWithAction_ReturnsResponseExample()
    {
        var service = CreateService();
        using var doc = JsonDocument.Parse("""
        {
          "action": "goal_create"
        }
        """);

        var result = await service.ExecuteAsync("help_action_schema", doc.RootElement);

        result["responseExample"].Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_HelpActionSchemaWithAction_ExposesEnumAndRangeMetadata()
    {
        var service = CreateService();
        using var missionDoc = JsonDocument.Parse("""{"action":"goal_create"}""");
        using var checkinDoc = JsonDocument.Parse("""{"action":"indicator_checkin_create"}""");

        var missionResult = await service.ExecuteAsync("help_action_schema", missionDoc.RootElement);
        var checkinResult = await service.ExecuteAsync("help_action_schema", checkinDoc.RootElement);

        var statusSchema = missionResult["inputSchema"]!["properties"]!["status"]!;
        statusSchema["enum"]!.AsArray().Should().NotBeEmpty();
        statusSchema["x-enumNames"]!.AsArray().Should().Contain(n => n!.GetValue<string>() == "Planned");

        var confidenceSchema = checkinResult["inputSchema"]!["properties"]!["confidenceLevel"]!;
        confidenceSchema["minimum"]!.GetValue<int>().Should().Be(1);
        confidenceSchema["maximum"]!.GetValue<int>().Should().Be(5);
    }

    [Fact]
    public async Task ExecuteAsync_MissionUpdateWithNullableField_DoesNotThrowNodeTypeError()
    {
        var service = CreateDomainReadyService();
        using var loginDoc = JsonDocument.Parse("""{"email":"user@getbud.co"}""");
        var loginResult = await service.ExecuteAsync("auth_login", loginDoc.RootElement);
        var tenantId = loginResult["whoami"]!["organizationId"]!.GetValue<string>();
        using var tenantDoc = JsonDocument.Parse($$"""{"tenantId":"{{tenantId}}"}""");
        await service.ExecuteAsync("tenant_set_current", tenantDoc.RootElement);

        using var updateDoc = JsonDocument.Parse("""
        {
          "id": "00000000-0000-0000-0000-000000000002",
          "payload": {
            "name": "Missão atualizada",
            "description": "Ajuste com campo nullable preenchido",
            "dimension": "Clientes",
            "startDate": "2026-02-08T00:00:00Z",
            "endDate": "2026-02-20T00:00:00Z",
            "status": "Active"
          }
        }
        """);

        var act = () => service.ExecuteAsync("goal_update", updateDoc.RootElement);
        await act.Should().NotThrowAsync();
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
            .Should().Contain(LoginNextSteps);
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
        result["nextSteps"]!.AsArray().Select(i => i!.GetValue<string>())
            .Should().Contain("help_list_actions");
        result["starterSchemas"]!.AsArray()
            .Select(item => item!["name"]!.GetValue<string>())
            .Should().Contain(StarterSchemaNames);
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
            if (request.RequestUri!.AbsolutePath == "/api/sessions")
            {
                return JsonResponse(new SessionResponse
                {
                    Token = "jwt-token",
                    Email = "user@getbud.co",
                    DisplayName = "Usuário"
                });
            }

            if (request.RequestUri.AbsolutePath == "/api/me/organizations")
            {
                return JsonResponse(new List<MyOrganizationResponse>
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

    private static McpToolService CreateDomainReadyService()
    {
        var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var goalId = Guid.Parse("00000000-0000-0000-0000-000000000002");

        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/sessions")
            {
                return JsonResponse(new SessionResponse
                {
                    Token = "jwt-token",
                    Email = "user@getbud.co",
                    DisplayName = "Usuário",
                    OrganizationId = tenantId
                });
            }

            if (request.RequestUri.AbsolutePath == "/api/me/organizations")
            {
                return JsonResponse(new List<MyOrganizationResponse>
                {
                    new() { Id = tenantId, Name = "Org 1" }
                });
            }

            if (request.Method == HttpMethod.Patch && request.RequestUri.AbsolutePath == $"/api/goals/{goalId}")
            {
                return JsonResponse(new GoalResponse
                {
                    Id = goalId,
                    Name = "Missão atualizada",
                    Description = "Ajuste com campo nullable preenchido",
                    StartDate = DateTime.Parse("2026-02-08T00:00:00Z", CultureInfo.InvariantCulture),
                    EndDate = DateTime.Parse("2026-02-20T00:00:00Z", CultureInfo.InvariantCulture),
                    Status = GoalStatus.Active,
                    OrganizationId = tenantId
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
