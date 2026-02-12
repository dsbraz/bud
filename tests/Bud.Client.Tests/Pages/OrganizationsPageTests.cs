using System.Net;
using System.Text;
using Bud.Client.Pages;
using Bud.Client.Services;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace Bud.Client.Tests.Pages;

public sealed class OrganizationsPageTests : TestContext
{
    [Fact]
    public void Render_WithGlobalAdminSession_ShouldShowPageHeader()
    {
        var authSessionJson = """
            {
              "Token":"token",
              "Email":"admin@getbud.co",
              "DisplayName":"Administrador Global",
              "IsGlobalAdmin":true
            }
            """;

        var jsRuntime = new SessionJsRuntime(authSessionJson);
        var handler = new RouteHandler(request =>
        {
            var path = request.RequestUri!.PathAndQuery;
            if (path.StartsWith("/api/organizations", StringComparison.Ordinal))
            {
                return Json("""{"items":[],"total":0,"page":1,"pageSize":20}""");
            }

            return Json("""[]""");
        });

        Services.AddSingleton<IJSRuntime>(jsRuntime);
        Services.AddSingleton(new ToastService());
        Services.AddSingleton(new AuthState(jsRuntime));
        Services.AddSingleton(new OrganizationContext(jsRuntime));
        Services.AddSingleton(new ApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") }, new ToastService()));

        var cut = RenderComponent<Organizations>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Organizações");
            cut.Markup.Should().Contain("Nova organização");
        });
    }

    private static HttpResponseMessage Json(string json)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class RouteHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }

    private sealed class SessionJsRuntime(string authSessionJson) : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier == "localStorage.getItem" &&
                args is { Length: > 0 } &&
                string.Equals(args[0]?.ToString(), "bud.auth.session", StringComparison.Ordinal))
            {
                return new ValueTask<TValue>((TValue)(object)authSessionJson);
            }

            if (identifier == "localStorage.getItem")
            {
                return new ValueTask<TValue>((TValue)(object)string.Empty);
            }

            return new ValueTask<TValue>(default(TValue)!);
        }
    }
}
