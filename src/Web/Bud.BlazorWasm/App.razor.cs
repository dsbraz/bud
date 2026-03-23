using Bud.BlazorWasm.State;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Bud.BlazorWasm;

public partial class App
{
    [Inject] private AuthState AuthState { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private async Task HandleNavigationAsync(NavigationContext context)
    {
        await AuthState.EnsureInitializedAsync();

        var path = context.Path ?? string.Empty;
        var isLogin = path.StartsWith("login", StringComparison.OrdinalIgnoreCase);

        if (!AuthState.IsAuthenticated && !isLogin)
        {
            var returnUrl = Uri.EscapeDataString(GetCurrentRelativeUrl());
            Nav.NavigateTo($"login?returnUrl={returnUrl}");
            return;
        }

        if (AuthState.IsAuthenticated && isLogin)
        {
            Nav.NavigateTo("/");
        }
    }

    private string GetCurrentRelativeUrl()
    {
        var uri = Nav.ToAbsoluteUri(Nav.Uri);
        return string.IsNullOrWhiteSpace(uri.PathAndQuery) ? "/" : uri.PathAndQuery;
    }
}
