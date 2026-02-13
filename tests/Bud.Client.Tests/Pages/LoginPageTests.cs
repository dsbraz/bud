using Bud.Client.Pages;
using Bud.Client.Services;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace Bud.Client.Tests.Pages;

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

    private sealed class StubJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => new((TValue)(object)string.Empty);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => new((TValue)(object)string.Empty);
    }
}
