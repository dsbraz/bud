using System.Text.Json;
using Bud.Mcp.Tests.Helpers;

namespace Bud.Mcp.Tests.Auth;

public sealed class BudApiSessionTests
{
    [Fact]
    public async Task InitializeAsync_WithoutConfiguredUser_DoesNotAuthenticate()
    {
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("Não deveria chamar API."));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://bud.test") };
        var options = new BudMcpOptions("http://bud.test", null, null, 30);
        var session = new BudApiSession(httpClient, options);

        await session.InitializeAsync();

        session.AuthContext.Should().BeNull();
    }

    [Fact]
    public async Task InitializeAsync_WithValidLogin_StoresAuthContext()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/sessions")
            {
                return JsonResponse(new AuthLoginResponse
                {
                    Token = "jwt-token",
                    Email = "user@getbud.co",
                    DisplayName = "Usuário",
                    IsGlobalAdmin = false,
                    CollaboratorId = Guid.NewGuid(),
                    OrganizationId = Guid.NewGuid()
                });
            }

            throw new InvalidOperationException("Request inesperada.");
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://bud.test") };
        var options = new BudMcpOptions("http://bud.test", "user@getbud.co", null, 30);
        var session = new BudApiSession(httpClient, options);

        await session.InitializeAsync();

        session.AuthContext.Should().NotBeNull();
        session.AuthContext!.Token.Should().Be("jwt-token");
        session.AuthContext.Email.Should().Be("user@getbud.co");
    }

    [Fact]
    public async Task LoginAsync_WithValidEmail_StoresAuthContext()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/sessions")
            {
                return JsonResponse(new AuthLoginResponse
                {
                    Token = "jwt-dinamico",
                    Email = "maria@acme.com",
                    DisplayName = "Maria"
                });
            }

            throw new InvalidOperationException("Request inesperada.");
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://bud.test") };
        var options = new BudMcpOptions("http://bud.test", null, null, 30);
        var session = new BudApiSession(httpClient, options);

        await session.InitializeAsync();
        await session.LoginAsync("maria@acme.com");

        session.AuthContext.Should().NotBeNull();
        session.AuthContext!.Email.Should().Be("maria@acme.com");
        session.AuthContext.Token.Should().Be("jwt-dinamico");
    }

    [Fact]
    public async Task ListAvailableTenantsAsync_AddsAuthorizationHeader()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/sessions")
            {
                return JsonResponse(new AuthLoginResponse
                {
                    Token = "jwt-token",
                    Email = "user@getbud.co",
                    DisplayName = "Usuário"
                });
            }

            if (request.RequestUri.AbsolutePath == "/api/me/organizations")
            {
                request.Headers.Authorization.Should().NotBeNull();
                request.Headers.Authorization!.Scheme.Should().Be("Bearer");
                request.Headers.Authorization.Parameter.Should().Be("jwt-token");

                return JsonResponse(new List<OrganizationSummaryDto>
                {
                    new() { Id = Guid.NewGuid(), Name = "Org 1" }
                });
            }

            throw new InvalidOperationException("Request inesperada.");
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://bud.test") };
        var options = new BudMcpOptions("http://bud.test", "user@getbud.co", null, 30);
        var session = new BudApiSession(httpClient, options);
        await session.InitializeAsync();

        var orgs = await session.ListAvailableTenantsAsync();

        orgs.Should().HaveCount(1);
        orgs[0].Name.Should().Be("Org 1");
    }

    [Fact]
    public async Task SetCurrentTenantAsync_WithInvalidTenant_ThrowsValidationMessageInPortuguese()
    {
        var validTenantId = Guid.NewGuid();

        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/sessions")
            {
                return JsonResponse(new AuthLoginResponse
                {
                    Token = "jwt-token",
                    Email = "user@getbud.co",
                    DisplayName = "Usuário"
                });
            }

            if (request.RequestUri.AbsolutePath == "/api/me/organizations")
            {
                return JsonResponse(new List<OrganizationSummaryDto>
                {
                    new() { Id = validTenantId, Name = "Org Válida" }
                });
            }

            throw new InvalidOperationException("Request inesperada.");
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://bud.test") };
        var options = new BudMcpOptions("http://bud.test", "user@getbud.co", null, 30);
        var session = new BudApiSession(httpClient, options);
        await session.InitializeAsync();

        var act = () => session.SetCurrentTenantAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Tenant informado não está disponível para o usuário autenticado.");
    }

    private static HttpResponseMessage JsonResponse<T>(T payload)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload))
        };
    }
}
