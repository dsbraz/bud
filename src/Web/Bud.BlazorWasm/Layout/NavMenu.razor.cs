using Bud.BlazorWasm.State;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Bud.BlazorWasm.Layout;

public partial class NavMenu : IDisposable
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthState AuthState { get; set; } = default!;

    private bool _pessoasExpanded = true;
    private bool _configExpanded = true;

    private static readonly string[] PessoasRoutes = ["/collaborators", "/workspaces", "/teams"];
    private static readonly string[] ConfigRoutes = ["/goal-templates"];

    private string GetOrganizationsActiveClass()
    {
        var uri = Navigation.Uri;
        return uri.Contains("/organizations", StringComparison.OrdinalIgnoreCase)
            ? "active"
            : "";
    }

    private string GetPessoasActiveClass()
    {
        var uri = Navigation.Uri;
        return PessoasRoutes.Any(route => uri.Contains(route, StringComparison.OrdinalIgnoreCase))
            ? "active"
            : "";
    }

    private string GetSubitemActiveClass(string route)
    {
        return Navigation.Uri.Contains(route, StringComparison.OrdinalIgnoreCase)
            ? "active"
            : "";
    }

    private string GetConfigActiveClass()
    {
        var uri = Navigation.Uri;
        return ConfigRoutes.Any(route => uri.Contains(route, StringComparison.OrdinalIgnoreCase))
            ? "active"
            : "";
    }

    private string GetMetasActiveClass()
    {
        var uri = Navigation.Uri;
        var metasRoutes = new[] { "/goals" };
        return metasRoutes.Any(route => uri.Contains(route, StringComparison.OrdinalIgnoreCase))
            ? "active"
            : "";
    }

    private void TogglePessoasSubmenu()
    {
        _pessoasExpanded = !_pessoasExpanded;
    }

    private void ToggleConfigSubmenu()
    {
        _configExpanded = !_configExpanded;
    }

    protected override void OnInitialized()
    {
        Navigation.LocationChanged += OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        if (PessoasRoutes.Any(route => e.Location.Contains(route, StringComparison.OrdinalIgnoreCase)))
        {
            _pessoasExpanded = true;
        }
        if (ConfigRoutes.Any(route => e.Location.Contains(route, StringComparison.OrdinalIgnoreCase)))
        {
            _configExpanded = true;
        }
        StateHasChanged();
    }

    public void Dispose()
    {
        Navigation.LocationChanged -= OnLocationChanged;
        GC.SuppressFinalize(this);
    }
}
