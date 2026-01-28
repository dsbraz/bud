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
    public AuthSession? Session => _session;

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

    public async Task SetSessionAsync(AuthLoginResponse response)
    {
        _session = new AuthSession
        {
            Email = response.Email,
            DisplayName = response.DisplayName,
            IsAdmin = response.IsAdmin,
            CollaboratorId = response.CollaboratorId,
            Role = response.Role
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
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public Guid? CollaboratorId { get; set; }
    public Bud.Shared.Models.CollaboratorRole? Role { get; set; }
}
