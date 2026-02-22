using System.Text.Json;
using Bud.Shared.Contracts;
using Microsoft.JSInterop;

namespace Bud.Client.Services;

public sealed class AuthState(IJSRuntime jsRuntime)
{
    private const string StorageKey = "bud.auth.session";
    private bool _initialized;
    private AuthSession? _session;

    public bool IsAuthenticated => _session is not null;
    public AuthSession? SessionResponse => _session;

    public async Task EnsureInitializedAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        var json = await jsRuntime.InvokeAsync<string>("localStorage.getItem", StorageKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        try
        {
            _session = JsonSerializer.Deserialize<AuthSession>(json);
        }
        catch (JsonException)
        {
            _session = null;
        }
    }

    public async Task SetSessionAsync(SessionResponse response)
    {
        _session = new AuthSession
        {
            Token = response.Token,
            Email = response.Email,
            DisplayName = response.DisplayName,
            IsGlobalAdmin = response.IsGlobalAdmin,
            CollaboratorId = response.CollaboratorId,
            Role = response.Role,
            OrganizationId = response.OrganizationId
        };

        var json = JsonSerializer.Serialize(_session);
        await jsRuntime.InvokeAsync<object>("localStorage.setItem", StorageKey, json);
    }

    public async Task ClearAsync()
    {
        _session = null;
        await jsRuntime.InvokeAsync<object>("localStorage.removeItem", StorageKey);
    }
}

public sealed class AuthSession
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsGlobalAdmin { get; set; }
    public Guid? CollaboratorId { get; set; }
    public Bud.Shared.Contracts.CollaboratorRole? Role { get; set; }
    public Guid? OrganizationId { get; set; }
}
