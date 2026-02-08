using Bud.Mcp.Tests.Helpers;
using Bud.Shared.Models;
using System.Text.Json;

namespace Bud.Mcp.Tests.Http;

public sealed class BudApiClientTests
{
    [Fact]
    public async Task CreateMissionAsync_WithoutAuthentication_ThrowsAuthMessageInPortuguese()
    {
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("Request inesperada."));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://bud.test") };
        var options = new BudMcpOptions("http://bud.test", null, null, 30);
        var session = new BudApiSession(httpClient, options);
        await session.InitializeAsync();

        var client = new BudApiClient(httpClient, session);
        var act = () => client.CreateMissionAsync(new CreateMissionRequest
        {
            Name = "Missão Teste",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = MissionStatus.Active,
            ScopeType = MissionScopeType.Organization,
            ScopeId = Guid.NewGuid()
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Sessão MCP não autenticada. Execute auth_login informando um e-mail válido.");
    }

    [Fact]
    public async Task CreateMissionAsync_SendsAuthorizationAndTenantHeaders()
    {
        var tenantId = Guid.NewGuid();
        var responseMissionId = Guid.NewGuid();

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

            if (request.RequestUri.AbsolutePath == "/api/missions" && request.Method == HttpMethod.Post)
            {
                request.Headers.Authorization.Should().NotBeNull();
                request.Headers.Authorization!.Scheme.Should().Be("Bearer");
                request.Headers.Authorization.Parameter.Should().Be("jwt-token");

                request.Headers.TryGetValues("X-Tenant-Id", out var tenantValues).Should().BeTrue();
                tenantValues!.Single().Should().Be(tenantId.ToString());

                var payload = JsonDocument.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult()).RootElement;
                payload.TryGetProperty("request", out _).Should().BeFalse();
                payload.GetProperty("status").ValueKind.Should().Be(JsonValueKind.Number);
                payload.GetProperty("status").GetInt32().Should().Be((int)MissionStatus.Active);
                payload.GetProperty("scopeType").ValueKind.Should().Be(JsonValueKind.Number);
                payload.GetProperty("scopeType").GetInt32().Should().Be((int)MissionScopeType.Organization);

                return JsonResponse(new Mission
                {
                    Id = responseMissionId,
                    Name = "Missão Teste",
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(30),
                    Status = MissionStatus.Active,
                    OrganizationId = tenantId
                });
            }

            throw new InvalidOperationException("Request inesperada.");
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://bud.test") };
        var options = new BudMcpOptions("http://bud.test", "user@getbud.co", null, 30);
        var session = new BudApiSession(httpClient, options);
        await session.InitializeAsync();
        await session.SetCurrentTenantAsync(tenantId);

        var client = new BudApiClient(httpClient, session);
        var mission = await client.CreateMissionAsync(new CreateMissionRequest
        {
            Name = "Missão Teste",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = MissionStatus.Active,
            ScopeType = MissionScopeType.Organization,
            ScopeId = tenantId
        });

        mission.Id.Should().Be(responseMissionId);
    }

    [Fact]
    public async Task CreateMissionAsync_WithoutTenantSelected_ThrowsValidationMessageInPortuguese()
    {
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

            throw new InvalidOperationException("Request inesperada.");
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://bud.test") };
        var options = new BudMcpOptions("http://bud.test", "user@getbud.co", null, 30);
        var session = new BudApiSession(httpClient, options);
        await session.InitializeAsync();

        var client = new BudApiClient(httpClient, session);
        var act = () => client.CreateMissionAsync(new CreateMissionRequest
        {
            Name = "Missão Teste",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = MissionStatus.Active,
            ScopeType = MissionScopeType.Organization,
            ScopeId = Guid.NewGuid()
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Selecione um tenant antes de executar operações de domínio.");
    }

    private static HttpResponseMessage JsonResponse<T>(T payload)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload))
        };
    }
}
