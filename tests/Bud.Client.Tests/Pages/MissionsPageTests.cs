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

public sealed class MissionsPageTests : TestContext
{
    [Fact]
    public async Task HandleClearFilters_ShouldRestoreActiveOnlyDefaultTrue()
    {
        var cut = RenderMissionsPage();
        var instance = cut.Instance;

        SetField(instance, "_filterActiveOnly", false);
        SetField(instance, "_filterScopeTypeValue", "Team");
        SetField(instance, "_filterScopeId", Guid.NewGuid().ToString());
        SetField(instance, "_search", "abc");

        await InvokePrivateTask(instance, "HandleClearFilters");

        GetField<bool>(instance, "_filterActiveOnly").Should().BeTrue();
        GetField<string?>(instance, "_filterScopeTypeValue").Should().BeNull();
        GetField<string?>(instance, "_filterScopeId").Should().BeNull();
        GetField<string?>(instance, "_search").Should().BeNull();
    }

    [Fact]
    public void Render_ShouldShowPageHeader()
    {
        var cut = RenderMissionsPage();

        cut.Markup.Should().Contain("Miss");
        cut.Markup.Should().Contain("Criar miss");
    }

    [Fact]
    public void Render_ShouldNotShowMissionsTopMenu()
    {
        var cut = RenderMissionsPage();

        cut.Markup.Should().NotContain("management-menu");
    }

    [Fact]
    public void Render_InitialState_ShouldNotShowClearFilterChip()
    {
        var cut = RenderMissionsPage();

        cut.Markup.Should().NotContain("Limpar filtro");
    }

    private IRenderedComponent<Missions> RenderMissionsPage()
    {
        var authSessionJson = """
            {
              "Token":"token",
              "Email":"user@getbud.co",
              "DisplayName":"Usuário",
              "IsGlobalAdmin":false,
              "CollaboratorId":"11111111-1111-1111-1111-111111111111"
            }
            """;

        var jsRuntime = new SessionJsRuntime(authSessionJson);
        var handler = new RouteHandler(request =>
        {
            var path = request.RequestUri!.PathAndQuery;

            if (path.StartsWith("/api/organizations", StringComparison.Ordinal))
            {
                return Json("""{"items":[{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Org"}],"total":1,"page":1,"pageSize":100}""");
            }

            if (path.StartsWith("/api/workspaces", StringComparison.Ordinal) ||
                path.StartsWith("/api/teams", StringComparison.Ordinal) ||
                path.StartsWith("/api/collaborators", StringComparison.Ordinal) ||
                path.StartsWith("/api/mission-templates", StringComparison.Ordinal) ||
                path.StartsWith("/api/objective-dimensions", StringComparison.Ordinal))
            {
                return Json("""{"items":[],"total":0,"page":1,"pageSize":100}""");
            }

            if (path.StartsWith("/api/missions/my", StringComparison.Ordinal) || path.StartsWith("/api/missions", StringComparison.Ordinal))
            {
                return Json("""{"items":[],"total":0,"page":1,"pageSize":100}""");
            }

            if (path.StartsWith("/api/missions/progress", StringComparison.Ordinal))
            {
                return Json("[]");
            }

            return Json("[]");
        });

        var toastService = new ToastService();
        Services.AddSingleton<IJSRuntime>(jsRuntime);
        Services.AddSingleton(toastService);
        Services.AddSingleton(new AuthState(jsRuntime));
        Services.AddSingleton(new OrganizationContext(jsRuntime));
        Services.AddSingleton(new ApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") }, toastService));
        Services.AddSingleton(new UiOperationService(toastService));

        return RenderComponent<Missions>();
    }

    private static T GetField<T>(object instance, string name)
        => (T)instance.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(instance)!;

    private static void SetField<T>(object instance, string name, T value)
        => instance.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(instance, value);

    private static async Task InvokePrivateTask(object instance, string methodName)
    {
        var method = instance.GetType().GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var result = method.Invoke(instance, Array.Empty<object>());
        if (result is Task task)
        {
            await task;
        }
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
                if (args is { Length: > 0 } && string.Equals(args[0]?.ToString(), "bud.selected.organization", StringComparison.Ordinal))
                {
                    return new ValueTask<TValue>((TValue)(object)"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
                }

                return new ValueTask<TValue>((TValue)(object)string.Empty);
            }

            return new ValueTask<TValue>(default(TValue)!);
        }
    }
}
