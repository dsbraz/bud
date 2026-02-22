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

public sealed class MissionTemplatesPageTests : TestContext
{
    [Fact]
    public void OpenCreateModal_ShouldUseMissionTabLabelInsteadOfModelo()
    {
        var cut = RenderMissionTemplatesPage();
        var instance = cut.Instance;

        InvokePrivateVoid(instance, "OpenCreateModal");
        cut.Render();

        cut.Markup.Should().Contain("Missão");
        cut.Markup.Should().NotContain("<span>Modelo</span>");
    }

    [Fact]
    public void OpenCreateModal_ShouldNotShowMissionPatternsSection()
    {
        var cut = RenderMissionTemplatesPage();
        var instance = cut.Instance;

        InvokePrivateVoid(instance, "OpenCreateModal");
        cut.Render();

        cut.Markup.Should().NotContain("Padrões da missão");
    }

    private IRenderedComponent<MissionTemplates> RenderMissionTemplatesPage()
    {
        var jsRuntime = new SessionJsRuntime();
        var handler = new RouteHandler(request =>
        {
            var path = request.RequestUri!.PathAndQuery;

            if (path.StartsWith("/api/templates", StringComparison.Ordinal))
            {
                return Json("""{"items":[],"total":0,"page":1,"pageSize":20}""");
            }

            return Json("[]");
        });

        var toastService = new ToastService();
        Services.AddSingleton<IJSRuntime>(jsRuntime);
        Services.AddSingleton(toastService);
        Services.AddSingleton(new OrganizationContext(jsRuntime));
        Services.AddSingleton(new ApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") }, toastService));
        Services.AddSingleton(new UiOperationService(toastService));

        return RenderComponent<MissionTemplates>();
    }

    private static void InvokePrivateVoid(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        method.Invoke(instance, args);
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

    private sealed class SessionJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier == "localStorage.getItem" &&
                args is { Length: > 0 } &&
                string.Equals(args[0]?.ToString(), "bud.selected.organization", StringComparison.Ordinal))
            {
                return new ValueTask<TValue>((TValue)(object)"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            }

            if (identifier == "localStorage.getItem")
            {
                return new ValueTask<TValue>((TValue)(object)string.Empty);
            }

            return new ValueTask<TValue>(default(TValue)!);
        }
    }
}
