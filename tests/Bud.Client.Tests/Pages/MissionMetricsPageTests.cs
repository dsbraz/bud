using System.Net;
using System.Text;
using Bud.Client.Pages;
using Bud.Client.Services;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace Bud.Client.Tests.Pages;

public sealed class MissionMetricsPageTests : TestContext
{
    [Fact]
    public void TryPrepareMetricForCreate_WhenMissionMissing_ShouldReturnFalse()
    {
        var cut = RenderMissionMetricsPage();
        var instance = cut.Instance;

        SetField<string?>(instance, "createMissionId", null);
        SetField(instance, "createMetricTypeValue", "Qualitative");

        var newMetric = GetField<CreateMissionMetricRequest>(instance, "newMetric");
        newMetric.Name = "Métrica";
        newMetric.TargetText = "Objetivo";

        var result = InvokePrivateBool(instance, "TryPrepareMetricForCreate", "Erro ao criar métrica");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task OnMetricTypeChanged_ShouldResetSpecificFields()
    {
        var cut = RenderMissionMetricsPage();
        var instance = cut.Instance;

        var newMetric = GetField<CreateMissionMetricRequest>(instance, "newMetric");
        newMetric.TargetText = "Texto";
        newMetric.MinValue = 1;
        newMetric.MaxValue = 10;
        newMetric.QuantitativeType = QuantitativeMetricType.Achieve;

        SetField(instance, "createQuantitativeTypeValue", "Achieve");
        SetField(instance, "createMetricUnitValue", "Points");

        await InvokePrivateTask(instance, "OnMetricTypeChanged", new ChangeEventArgs { Value = "Qualitative" });

        newMetric.TargetText.Should().BeNull();
        newMetric.MinValue.Should().BeNull();
        newMetric.MaxValue.Should().BeNull();
        newMetric.QuantitativeType.Should().BeNull();
        GetField<string?>(instance, "createQuantitativeTypeValue").Should().BeNull();
        GetField<string?>(instance, "createMetricUnitValue").Should().BeNull();
    }

    private IRenderedComponent<MissionMetrics> RenderMissionMetricsPage()
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

            if (path.StartsWith("/api/missions", StringComparison.Ordinal))
            {
                return Json("""{"items":[{"id":"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb","name":"Missão A","organizationId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"}],"total":1,"page":1,"pageSize":100}""");
            }

            if (path.StartsWith("/api/mission-metrics/progress", StringComparison.Ordinal))
            {
                return Json("[]");
            }

            if (path.StartsWith("/api/mission-metrics", StringComparison.Ordinal))
            {
                return Json("""{"items":[],"total":0,"page":1,"pageSize":100}""");
            }

            return Json("[]");
        });

        var toastService = new ToastService();
        Services.AddSingleton<IJSRuntime>(jsRuntime);
        Services.AddSingleton(toastService);
        Services.AddSingleton(new OrganizationContext(jsRuntime));
        Services.AddSingleton(new ApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") }, toastService));
        Services.AddSingleton(new UiOperationService(toastService));

        return RenderComponent<MissionMetrics>();
    }

    private static T GetField<T>(object instance, string name)
        => (T)instance.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(instance)!;

    private static void SetField<T>(object instance, string name, T value)
        => instance.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(instance, value);

    private static bool InvokePrivateBool(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        return (bool)method.Invoke(instance, args)!;
    }

    private static async Task InvokePrivateTask(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        if (method.Invoke(instance, args) is Task task)
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
