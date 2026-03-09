using Bud.BlazorWasm.Api;
using Bud.BlazorWasm.Features.Auth.Pages;
using Bud.BlazorWasm.Services;
using Bud.BlazorWasm.State;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Bud.BlazorWasm.Tests.Features.Auth;

public sealed class LoginPageTests : TestContext
{
    [Fact]
    public void Render_ShouldShowLoginFields()
    {
        Services.AddSingleton(new ToastService());
        Services.AddSingleton<IJSRuntime>(new StubJsRuntime());
        Services.AddSingleton(new AuthState(new StubJsRuntime()));
        Services.AddSingleton(new ApiClient(new HttpClient { BaseAddress = new Uri("http://localhost") }, new ToastService()));

        var cut = RenderComponent<Login>();

        cut.Markup.Should().Contain("Entrar");
        cut.Markup.Should().Contain("E-mail");
    }

    [Fact]
    public void Submit_WithValidEmail_ShouldNavigateToRoot()
    {
        var jsRuntime = new StubJsRuntime();
        var toastService = new ToastService();

        Services.AddSingleton(toastService);
        Services.AddSingleton<IJSRuntime>(jsRuntime);
        Services.AddSingleton(new AuthState(jsRuntime));
        Services.AddSingleton(new ApiClient(
            new HttpClient(new StubHttpMessageHandler()) { BaseAddress = new Uri("http://localhost") },
            toastService));

        var navigationManager = Services.GetRequiredService<NavigationManager>();

        var cut = RenderComponent<Login>();

        cut.Find("input").Change("admin@getbud.co");
        cut.Find("form").Submit();

        navigationManager.Uri.Should().Be("http://localhost/");
    }

    private sealed class StubJsRuntime : IJSRuntime
    {
        private readonly Dictionary<string, string?> _storage = new();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => new((TValue)(object)string.Empty);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier == "localStorage.getItem")
            {
                var key = args?[0]?.ToString() ?? string.Empty;
                _storage.TryGetValue(key, out var value);
                return new ValueTask<TValue>((TValue)(object)(value ?? string.Empty));
            }

            if (identifier == "localStorage.setItem")
            {
                var key = args?[0]?.ToString() ?? string.Empty;
                var value = args?[1]?.ToString();
                _storage[key] = value;
                return new ValueTask<TValue>(default(TValue)!);
            }

            if (identifier == "localStorage.removeItem")
            {
                var key = args?[0]?.ToString() ?? string.Empty;
                _storage.Remove(key);
                return new ValueTask<TValue>(default(TValue)!);
            }

            return new ValueTask<TValue>(default(TValue)!);
        }
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Post && request.RequestUri?.PathAndQuery == "/api/sessions")
            {
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        {
                          "token":"token",
                          "email":"admin@getbud.co",
                          "displayName":"Administrador Global",
                          "isGlobalAdmin":true,
                          "collaboratorId":"71fb52c4-de36-4cc0-bd9a-ee74540b87ce",
                          "role":1,
                          "organizationId":"b6671699-d694-469d-a3d6-5eef807e5776"
                        }
                        """,
                        System.Text.Encoding.UTF8,
                        "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
        }
    }
}
