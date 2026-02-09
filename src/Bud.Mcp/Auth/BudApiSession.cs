using System.Net.Http.Json;
using Bud.Mcp.Configuration;
using Bud.Mcp.Http;
using Bud.Shared.Contracts;

namespace Bud.Mcp.Auth;

public sealed class BudApiSession(HttpClient httpClient, BudMcpOptions options)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly BudMcpOptions _options = options;
    private List<OrganizationSummaryDto>? _cachedOrganizations;

    public BudAuthContext? AuthContext { get; private set; }
    public Guid? CurrentTenantId { get; private set; }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.UserEmail))
        {
            return;
        }

        await LoginAsync(_options.UserEmail, cancellationToken);
    }

    public async Task LoginAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("E-mail é obrigatório para autenticação.");
        }

        var loginResponse = await _httpClient.PostAsJsonAsync(
            "/api/auth/login",
            new AuthLoginRequest { Email = email.Trim() },
            cancellationToken);

        var loginPayload = await ReadSuccessResponseOrThrowAsync<AuthLoginResponse>(loginResponse, cancellationToken);
        if (string.IsNullOrWhiteSpace(loginPayload.Token))
        {
            throw new InvalidOperationException("Falha ao autenticar: token JWT não foi retornado pela API.");
        }

        AuthContext = BudAuthContext.FromResponse(loginPayload);
        CurrentTenantId = null;
        _cachedOrganizations = null;

        if (_options.DefaultTenantId.HasValue)
        {
            await SetCurrentTenantAsync(_options.DefaultTenantId.Value, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<OrganizationSummaryDto>> ListAvailableTenantsAsync(CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        var request = CreateBaseRequest(HttpMethod.Get, "/api/auth/my-organizations");
        request.Headers.TryAddWithoutValidation("X-User-Email", AuthContext!.Email);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var organizations = await ReadSuccessResponseOrThrowAsync<List<OrganizationSummaryDto>>(response, cancellationToken);
        _cachedOrganizations = organizations;
        return organizations;
    }

    public async Task SetCurrentTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var organizations = _cachedOrganizations ?? [.. await ListAvailableTenantsAsync(cancellationToken)];
        var hasTenant = organizations.Any(o => o.Id == tenantId);
        if (!hasTenant)
        {
            throw new InvalidOperationException("Tenant informado não está disponível para o usuário autenticado.");
        }

        CurrentTenantId = tenantId;
    }

    public HttpRequestMessage CreateDomainRequest(HttpMethod method, string relativePath, bool requireTenant = true)
    {
        EnsureAuthenticated();

        if (requireTenant && !CurrentTenantId.HasValue)
        {
            throw new InvalidOperationException("Selecione um tenant antes de executar operações de domínio.");
        }

        var request = CreateBaseRequest(method, relativePath);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AuthContext!.Token);
        if (CurrentTenantId.HasValue)
        {
            request.Headers.TryAddWithoutValidation("X-Tenant-Id", CurrentTenantId.Value.ToString());
        }

        return request;
    }

    private static HttpRequestMessage CreateBaseRequest(HttpMethod method, string relativePath)
    {
        return new HttpRequestMessage(method, relativePath);
    }

    private void EnsureAuthenticated()
    {
        if (AuthContext is null)
        {
            throw new InvalidOperationException("Sessão MCP não autenticada. Execute auth_login informando um e-mail válido.");
        }
    }

    private static async Task<T> ReadSuccessResponseOrThrowAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw await BudApiException.FromHttpResponseAsync(response, cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        if (result is null)
        {
            throw new InvalidOperationException("Resposta da API inválida ou vazia.");
        }

        return result;
    }
}
