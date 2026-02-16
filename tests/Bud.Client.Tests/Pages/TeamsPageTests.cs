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

public sealed class TeamsPageTests : TestContext
{
    [Fact]
    public void HasActiveFilters_WhenSearchIsFilled_ShouldReturnTrue()
    {
        var cut = RenderTeamsPage();
        var instance = cut.Instance;

        SetField<string?>(instance, "search", "produto");
        SetField<string?>(instance, "selectedWorkspaceId", null);

        var result = InvokePrivateBool(instance, "HasActiveFilters");

        result.Should().BeTrue();
    }

    [Fact]
    public void Render_ShouldShowPageTitleAndFilterButton()
    {
        var cut = RenderTeamsPage();

        cut.Markup.Should().Contain("Equipes");
        cut.Markup.Should().Contain("Filtrar");
    }

    private IRenderedComponent<Teams> RenderTeamsPage()
    {
        var jsRuntime = new StubJsRuntime();
        var handler = new RouteHandler(request =>
        {
            var path = request.RequestUri!.PathAndQuery;
            if (path.StartsWith("/api/workspaces", StringComparison.Ordinal))
            {
                return Json("""{"items":[],"total":0,"page":1,"pageSize":100}""");
            }

            if (path.StartsWith("/api/teams", StringComparison.Ordinal))
            {
                return Json("""{"items":[],"total":0,"page":1,"pageSize":20}""");
            }

            if (path.StartsWith("/api/collaborators", StringComparison.Ordinal))
            {
                return Json("""{"items":[],"total":0,"page":1,"pageSize":100}""");
            }

            if (path.StartsWith("/api/leaders", StringComparison.Ordinal))
            {
                return Json("[]");
            }

            if (path.StartsWith("/api/team-collaborators", StringComparison.Ordinal))
            {
                return Json("[]");
            }

            if (path.StartsWith("/api/collaborator-summaries", StringComparison.Ordinal))
            {
                return Json("[]");
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var toastService = new ToastService();
        Services.AddSingleton<IJSRuntime>(jsRuntime);
        Services.AddSingleton(toastService);
        Services.AddSingleton(new OrganizationContext(jsRuntime));
        Services.AddSingleton(new ApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") }, toastService));
        Services.AddSingleton(new UiOperationService(toastService));

        return RenderComponent<Teams>();
    }

    private static void SetField<T>(object instance, string name, T value)
        => instance.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(instance, value);

    private static bool InvokePrivateBool(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        return (bool)method.Invoke(instance, args)!;
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

    private sealed class StubJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => new((TValue)(object)string.Empty);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => new((TValue)(object)string.Empty);
    }
}
