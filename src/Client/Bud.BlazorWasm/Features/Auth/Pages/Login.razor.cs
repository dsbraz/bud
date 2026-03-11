using Bud.BlazorWasm.Api;
using Bud.BlazorWasm.Services;
using Bud.BlazorWasm.State;
using Bud.Shared.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace Bud.BlazorWasm.Features.Auth.Pages;

public partial class Login
{
    [Inject] private ApiClient Api { get; set; } = default!;
    [Inject] private AuthState AuthState { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private ToastService ToastService { get; set; } = default!;

    private readonly CreateSessionRequest login = new();
    private bool isSubmitting;

    protected override async Task OnInitializedAsync()
    {
        await AuthState.EnsureInitializedAsync();
        if (AuthState.IsAuthenticated)
        {
            Nav.NavigateTo("/");
        }
    }

    private async Task HandleLogin()
    {
        if (string.IsNullOrWhiteSpace(login.Email))
        {
            ToastService.ShowError("Erro ao fazer login", "Informe o e-mail para continuar.");
            return;
        }

        isSubmitting = true;
        try
        {
            var response = await Api.LoginAsync(login);
            if (response is null)
            {
                ToastService.ShowError("Erro ao fazer login", "Usuário não encontrado. Verifique o e-mail.");
                return;
            }

            await AuthState.SetSessionAsync(response);

            var returnUrl = GetReturnUrl();
            Nav.NavigateTo(returnUrl ?? "/");
        }
        finally
        {
            isSubmitting = false;
        }
    }

    private string? GetReturnUrl()
    {
        var uri = Nav.ToAbsoluteUri(Nav.Uri);
        if (string.IsNullOrWhiteSpace(uri.Query))
        {
            return null;
        }

        var query = QueryHelpers.ParseQuery(uri.Query);
        if (query.TryGetValue("returnUrl", out var returnUrl))
        {
            var value = returnUrl.ToString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
