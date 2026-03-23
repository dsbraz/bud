using Bud.BlazorWasm.Api;
using Bud.BlazorWasm.Services;
using Bud.BlazorWasm.State;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Bud.BlazorWasm.Layout;

public partial class MainLayout : IDisposable
{
    [Inject] private AuthState AuthState { get; set; } = default!;
    [Inject] private OrganizationContext OrgContext { get; set; } = default!;
    [Inject] private ApiClient Api { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private ToastService ToastService { get; set; } = default!;

    private bool _navOpen;
    private bool isDropdownOpen;
    private int unreadNotificationCount;
    private bool CanShowAllOrganizations => AuthState.SessionResponse?.IsGlobalAdmin == true;

    private string UserLabel => AuthState.SessionResponse is null
        ? "Sistema interno"
        : $"{AuthState.SessionResponse.DisplayName} · {AuthState.SessionResponse.Email}";

    private void ToggleNav() => _navOpen = !_navOpen;
    private void CloseNav() { _navOpen = false; }

    protected override async Task OnInitializedAsync()
    {
        Nav.LocationChanged += OnLocationChanged;
        await AuthState.EnsureInitializedAsync();

        try
        {
            var organizations = await Api.GetMyOrganizationsAsync();
            if (organizations != null)
            {
                await OrgContext.InitializeAsync(organizations);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao carregar organizações do usuário: {ex.Message}");
            ToastService.ShowWarning("Aviso", "Não foi possível carregar as organizações agora. Tente novamente em instantes.");
        }

        try
        {
            var unreadResult = await Api.GetNotificationsAsync(isRead: false, page: 1, pageSize: 1);
            if (unreadResult != null)
            {
                unreadNotificationCount = unreadResult.Total;
            }
        }
        catch
        {
            // Silently handle — notification count is non-critical
        }
    }

    private void OnUnreadCountChanged(int newCount)
    {
        unreadNotificationCount = newCount;
        StateHasChanged();
    }

    private void ToggleDropdown()
    {
        isDropdownOpen = !isDropdownOpen;
    }

    private async Task SelectOrganization(Guid? orgId)
    {
        await OrgContext.SelectOrganizationAsync(orgId);
        isDropdownOpen = false;
    }

    private string GetOrganizationInitials()
    {
        if (OrgContext.ShowAllOrganizations && CanShowAllOrganizations)
        {
            return "TD";
        }

        return GetInitials(OrgContext.GetSelectedOrganizationName());
    }

    private string GetOrganizationDisplayName()
    {
        if (OrgContext.ShowAllOrganizations && !CanShowAllOrganizations)
        {
            return "Desconhecida";
        }

        return OrgContext.GetSelectedOrganizationName();
    }

    private static string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "??";
        }

        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 1)
        {
            return words[0].Substring(0, Math.Min(2, words[0].Length)).ToUpperInvariant();
        }

        return $"{words[0][0]}{words[^1][0]}".ToUpperInvariant();
    }

    private async Task HandleLogout()
    {
        await Api.LogoutAsync();
        await AuthState.ClearAsync();
        await OrgContext.ClearAsync();
        Nav.NavigateTo("login");
    }

    private string GetGreeting()
    {
        var hour = DateTime.Now.Hour;
        var userName = GetUserName();

        if (hour < 12)
        {
            return $"Bom dia, {userName}!";
        }
        else if (hour < 18)
        {
            return $"Boa tarde, {userName}!";
        }
        else
        {
            return $"Boa noite, {userName}!";
        }
    }

    private string GetUserName()
    {
        if (AuthState.SessionResponse is null)
        {
            return "Usuário";
        }

        var displayName = AuthState.SessionResponse.DisplayName;
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return "Usuário";
        }

        var firstName = displayName.Split(' ').FirstOrDefault();
        return firstName ?? "Usuário";
    }

    private string GetUserInitials()
    {
        if (AuthState.SessionResponse is null)
        {
            return "U";
        }

        var displayName = AuthState.SessionResponse.DisplayName;
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return "U";
        }

        var names = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (names.Length == 1)
        {
            return names[0].Substring(0, Math.Min(2, names[0].Length)).ToUpperInvariant();
        }

        return $"{names[0][0]}{names[^1][0]}".ToUpperInvariant();
    }

    private string GetUserRole()
    {
        if (AuthState.SessionResponse is null)
        {
            return "Sem perfil";
        }

        if (AuthState.SessionResponse.IsGlobalAdmin)
        {
            return "Administrador Global";
        }

        return AuthState.SessionResponse.Role switch
        {
            Bud.Shared.Kernel.Enums.CollaboratorRole.Leader => "Líder",
            Bud.Shared.Kernel.Enums.CollaboratorRole.IndividualContributor => "Colaborador",
            _ => "Sem perfil"
        };
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        _navOpen = false;
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        Nav.LocationChanged -= OnLocationChanged;
        GC.SuppressFinalize(this);
    }
}
