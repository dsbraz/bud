using System.Net;
using System.Text;
using System.Text.Json;
using Bud.Client.Services;
using Bud.Client.Shared.Goals;
using Bud.Shared.Contracts;
using Bud.Shared.Contracts.Responses;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bud.Client.Tests.Shared.Goals;

public sealed class GoalChildSectionTests : TestContext
{
    private static GoalResponse CreateGoal(string name = "Submeta Teste", Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = name,
        OrganizationId = Guid.NewGuid(),
        StartDate = new DateTime(2025, 1, 1),
        EndDate = new DateTime(2025, 3, 31),
        Status = GoalStatus.Active
    };

    private static IndicatorResponse CreateIndicator(Guid goalId, string name = "Indicador Teste") => new()
    {
        Id = Guid.NewGuid(),
        GoalId = goalId,
        Name = name,
        Type = IndicatorType.Quantitative,
        QuantitativeType = QuantitativeIndicatorType.Achieve,
        MaxValue = 100,
        Unit = IndicatorUnit.Percentage
    };

    private static string SerializePagedResult<T>(IReadOnlyList<T> items) =>
        JsonSerializer.Serialize(new { items, total = items.Count, page = 1, pageSize = 100 });

    private static string SerializeList<T>(List<T> items) =>
        JsonSerializer.Serialize(items);

    private void SetupServices(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new RouteHandler(responder);
        var toastService = new ToastService();
        Services.AddSingleton(toastService);
        Services.AddSingleton(new ApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") }, toastService));
    }

    private static HttpResponseMessage EmptyPagedResponse() =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"items":[],"total":0,"page":1,"pageSize":100}""", Encoding.UTF8, "application/json")
        };

    [Fact]
    public void Render_WhenCollapsed_ShouldShowNameButNotContent()
    {
        SetupServices(_ => EmptyPagedResponse());
        var goal = CreateGoal("Submeta Visível");

        var cut = RenderComponent<GoalChildSection>(parameters => parameters
            .Add(p => p.ChildGoal, goal)
            .Add(p => p.IsExpanded, false));

        cut.Markup.Should().Contain("Submeta Visível");
        cut.Markup.Should().NotContain("Carregando...");
        cut.Markup.Should().NotContain("subgoal-indicators");
    }

    [Fact]
    public void Render_WhenExpanded_ShouldShowIndicators()
    {
        var goal = CreateGoal("Submeta com Indicador");
        var indicator = CreateIndicator(goal.Id, "NPS Score");

        SetupServices(request =>
        {
            var url = request.RequestUri!.PathAndQuery;

            if (url.Contains($"/api/goals/{goal.Id}/children"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(SerializePagedResult<GoalResponse>([]), Encoding.UTF8, "application/json")
                };
            }

            if (url.Contains($"/api/goals/{goal.Id}/indicators"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(SerializePagedResult<IndicatorResponse>([indicator]), Encoding.UTF8, "application/json")
                };
            }

            return EmptyPagedResponse();
        });

        var cut = RenderComponent<GoalChildSection>(parameters => parameters
            .Add(p => p.ChildGoal, goal)
            .Add(p => p.IsExpanded, true));

        cut.WaitForState(() => !cut.Markup.Contains("Carregando..."), TimeSpan.FromSeconds(2));

        cut.Markup.Should().Contain("NPS Score");
    }

    [Fact]
    public void Render_WhenExpanded_ShouldShowChildGoalsRecursively()
    {
        var parentGoal = CreateGoal("Submeta Pai");
        var childGoal = CreateGoal("Sub-submeta Filha");

        SetupServices(request =>
        {
            var url = request.RequestUri!.PathAndQuery;

            if (url.Contains($"/api/goals/{parentGoal.Id}/children"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(SerializePagedResult<GoalResponse>([childGoal]), Encoding.UTF8, "application/json")
                };
            }

            if (url.Contains($"/api/goals/{parentGoal.Id}/indicators"))
            {
                return EmptyPagedResponse();
            }

            if (url.Contains("/api/goals/progress"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(SerializeList(new List<GoalProgressResponse>
                    {
                        new() { GoalId = childGoal.Id, OverallProgress = 50 }
                    }), Encoding.UTF8, "application/json")
                };
            }

            return EmptyPagedResponse();
        });

        var cut = RenderComponent<GoalChildSection>(parameters => parameters
            .Add(p => p.ChildGoal, parentGoal)
            .Add(p => p.IsExpanded, true));

        cut.WaitForState(() => !cut.Markup.Contains("Carregando..."), TimeSpan.FromSeconds(2));

        cut.Markup.Should().Contain("Sub-submeta Filha");
    }

    [Fact]
    public void Render_WhenExpandedWithNoChildrenNorIndicators_ShouldShowEmptyMessage()
    {
        var goal = CreateGoal("Submeta Vazia");

        SetupServices(request =>
        {
            var url = request.RequestUri!.PathAndQuery;

            if (url.Contains($"/api/goals/{goal.Id}/children"))
            {
                return EmptyPagedResponse();
            }

            if (url.Contains($"/api/goals/{goal.Id}/indicators"))
            {
                return EmptyPagedResponse();
            }

            return EmptyPagedResponse();
        });

        var cut = RenderComponent<GoalChildSection>(parameters => parameters
            .Add(p => p.ChildGoal, goal)
            .Add(p => p.IsExpanded, true));

        cut.WaitForState(() => !cut.Markup.Contains("Carregando..."), TimeSpan.FromSeconds(2));

        cut.Markup.Should().Contain("Nenhum indicador ou meta nesta missão.");
    }

    [Fact]
    public void Render_WhenExpanded_ShouldShowGoalDimension()
    {
        var goal = CreateGoal("Submeta Dimensão");
        goal.Dimension = "Financeiro";

        SetupServices(_ => EmptyPagedResponse());

        var cut = RenderComponent<GoalChildSection>(parameters => parameters
            .Add(p => p.ChildGoal, goal)
            .Add(p => p.IsExpanded, false));

        cut.Markup.Should().Contain("Financeiro");
    }

    private sealed class RouteHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}
