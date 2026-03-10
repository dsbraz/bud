using System.Net;
using System.Text;
using System.Text.Json;
using Bud.BlazorWasm.Api;
using Bud.BlazorWasm.Services;
using Bud.BlazorWasm.Features.Goals.Components;
using Bud.Shared.Contracts;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bud.BlazorWasm.Tests.Features.Goals.Components;

public sealed class GoalGridViewTests : TestContext
{
    private static GoalResponse CreateGoal(string name = "Meta Teste", Guid? id = null, string? dimension = null, GoalStatus status = GoalStatus.Active) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = name,
        Dimension = dimension,
        OrganizationId = Guid.NewGuid(),
        StartDate = new DateTime(2025, 1, 1),
        EndDate = new DateTime(2025, 12, 31),
        Status = status
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

    private static GoalProgressResponse CreateGoalProgress(
        Guid goalId,
        decimal progress = 72,
        decimal expected = 80,
        int directChildren = 0,
        int directIndicators = 0) => new()
    {
        GoalId = goalId,
        OverallProgress = progress,
        ExpectedProgress = expected,
        TotalIndicators = 2,
        IndicatorsWithCheckins = 2,
        DirectChildren = directChildren,
        DirectIndicators = directIndicators
    };

    private static IndicatorProgressResponse CreateIndicatorProgress(Guid indicatorId, decimal progress = 65) => new()
    {
        IndicatorId = indicatorId,
        Progress = progress,
        HasCheckins = true,
        Confidence = 4
    };

    private static PagedResult<GoalResponse> CreatePagedGoals(params GoalResponse[] goals) => new()
    {
        Items = goals,
        Total = goals.Length,
        Page = 1,
        PageSize = 100
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

    private static HttpResponseMessage JsonResponse<T>(T data) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json")
        };

    [Fact]
    public void Render_WithRootGoals_ShouldShowGoalCards()
    {
        SetupServices(_ => EmptyPagedResponse());

        var goal1 = CreateGoal("Missão Alpha");
        var goal2 = CreateGoal("Missão Beta");
        var progress1 = CreateGoalProgress(goal1.Id, 72);
        var progress2 = CreateGoalProgress(goal2.Id, 45);

        var progressDict = new Dictionary<Guid, GoalProgressResponse>
        {
            [goal1.Id] = progress1,
            [goal2.Id] = progress2
        };

        var cut = RenderComponent<GoalGridView>(parameters => parameters
            .Add(p => p.RootGoals, CreatePagedGoals(goal1, goal2))
            .Add(p => p.RootGoalProgress, progressDict));

        cut.Markup.Should().Contain("Missão Alpha");
        cut.Markup.Should().Contain("Missão Beta");
        cut.FindAll(".mission-card").Should().HaveCount(2);
    }

    [Fact]
    public void Render_GoalCard_ShouldShowProgressPercentage()
    {
        SetupServices(_ => EmptyPagedResponse());

        var goal = CreateGoal("Meta com Progresso");
        var progress = CreateGoalProgress(goal.Id, 72);

        var cut = RenderComponent<GoalGridView>(parameters => parameters
            .Add(p => p.RootGoals, CreatePagedGoals(goal))
            .Add(p => p.RootGoalProgress, new Dictionary<Guid, GoalProgressResponse> { [goal.Id] = progress }));

        cut.Markup.Should().Contain("72%");
    }

    [Fact]
    public void Render_GoalCard_ShouldShowDimensionInModal()
    {
        SetupServices(_ => EmptyPagedResponse());

        var goal = CreateGoal("Meta Financeira", dimension: "Financeiro");
        var progress = CreateGoalProgress(goal.Id);

        var cut = RenderComponent<GoalGridView>(parameters => parameters
            .Add(p => p.RootGoals, CreatePagedGoals(goal))
            .Add(p => p.RootGoalProgress, new Dictionary<Guid, GoalProgressResponse> { [goal.Id] = progress }));

        // Dimension is shown in the modal header after expanding
        cut.Find(".mission-card-expand-btn").Click();
        cut.WaitForState(() => cut.FindAll(".goal-grid-container").Count > 0, TimeSpan.FromSeconds(2));
        cut.Find(".goal-grid-container .goal-grid-details").TextContent.Should().Be("Financeiro");
    }

    [Fact]
    public void Render_DraftGoal_ShouldShowDraftTag()
    {
        SetupServices(_ => EmptyPagedResponse());

        var goal = CreateGoal("Meta Rascunho", status: GoalStatus.Planned);

        var cut = RenderComponent<GoalGridView>(parameters => parameters
            .Add(p => p.RootGoals, CreatePagedGoals(goal))
            .Add(p => p.RootGoalProgress, new Dictionary<Guid, GoalProgressResponse>()));

        cut.Markup.Should().Contain("RASCUNHO");
        cut.Find(".goal-grid-draft-tag").Should().NotBeNull();
    }

    [Fact]
    public void Render_ClickGoal_ShouldOpenContainerWithChildren()
    {
        var parentGoal = CreateGoal("Missão Principal");
        var childGoal = CreateGoal("Sub-Meta Filha");
        var childIndicator = CreateIndicator(parentGoal.Id, "Indicador Direto");

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
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(SerializePagedResult<IndicatorResponse>([childIndicator]), Encoding.UTF8, "application/json")
                };
            }

            if (url.Contains("/api/goals/progress"))
            {
                return JsonResponse(new List<GoalProgressResponse> { CreateGoalProgress(childGoal.Id, 50) });
            }

            if (url.Contains($"/api/indicators/{childIndicator.Id}/progress"))
            {
                return JsonResponse(CreateIndicatorProgress(childIndicator.Id, 65));
            }

            return EmptyPagedResponse();
        });

        var cut = RenderComponent<GoalGridView>(parameters => parameters
            .Add(p => p.RootGoals, CreatePagedGoals(parentGoal))
            .Add(p => p.RootGoalProgress, new Dictionary<Guid, GoalProgressResponse>
            {
                [parentGoal.Id] = CreateGoalProgress(parentGoal.Id)
            }));

        // Click the expand button to open the mission modal
        cut.Find(".mission-card-expand-btn").Click();

        // Container should appear with the goal name in header
        cut.WaitForState(() => cut.FindAll(".goal-grid-container").Count > 0, TimeSpan.FromSeconds(2));
        cut.Find(".goal-grid-container-name").TextContent.Should().Contain("Missão Principal");

        // Should show child goal and indicator inside the container
        cut.WaitForState(() => cut.Markup.Contains("Sub-Meta Filha"), TimeSpan.FromSeconds(2));
        cut.Markup.Should().Contain("Indicador Direto");
    }

    [Fact]
    public void Render_CloseContainer_ShouldReturnToRootGrid()
    {
        var parentGoal = CreateGoal("Missão Raiz");
        var childGoal = CreateGoal("Sub-Meta");

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
                return JsonResponse(new List<GoalProgressResponse> { CreateGoalProgress(childGoal.Id, 50) });
            }

            return EmptyPagedResponse();
        });

        var cut = RenderComponent<GoalGridView>(parameters => parameters
            .Add(p => p.RootGoals, CreatePagedGoals(parentGoal))
            .Add(p => p.RootGoalProgress, new Dictionary<Guid, GoalProgressResponse>
            {
                [parentGoal.Id] = CreateGoalProgress(parentGoal.Id)
            }));

        // Open goal container
        cut.Find(".mission-card-expand-btn").Click();
        cut.WaitForState(() => cut.FindAll(".goal-grid-container").Count > 0, TimeSpan.FromSeconds(2));

        // Click close button
        cut.Find(".goal-grid-close-btn").Click();

        // Should return to root (no container, mission card visible)
        cut.WaitForState(() => cut.FindAll(".goal-grid-container").Count == 0, TimeSpan.FromSeconds(2));
        cut.FindAll(".mission-card").Should().HaveCount(1);
        cut.Markup.Should().Contain("Missão Raiz");
    }

    [Fact]
    public void Render_NavigateDeeper_ShouldShowBreadcrumb()
    {
        var rootGoal = CreateGoal("Missão Raiz");
        var childGoal = CreateGoal("Sub-Meta");
        var grandchildGoal = CreateGoal("Sub-Sub-Meta");

        SetupServices(request =>
        {
            var url = request.RequestUri!.PathAndQuery;

            if (url.Contains($"/api/goals/{rootGoal.Id}/children"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(SerializePagedResult<GoalResponse>([childGoal]), Encoding.UTF8, "application/json")
                };
            }

            if (url.Contains($"/api/goals/{childGoal.Id}/children"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(SerializePagedResult<GoalResponse>([grandchildGoal]), Encoding.UTF8, "application/json")
                };
            }

            if (url.Contains("/api/goals/progress"))
            {
                return JsonResponse(new List<GoalProgressResponse>());
            }

            return EmptyPagedResponse();
        });

        var cut = RenderComponent<GoalGridView>(parameters => parameters
            .Add(p => p.RootGoals, CreatePagedGoals(rootGoal))
            .Add(p => p.RootGoalProgress, new Dictionary<Guid, GoalProgressResponse>()));

        // Open root goal
        cut.Find(".mission-card-expand-btn").Click();
        cut.WaitForState(() => cut.FindAll(".goal-grid-container").Count > 0, TimeSpan.FromSeconds(2));

        // Navigate into child
        cut.WaitForState(() => cut.Markup.Contains("Sub-Meta"), TimeSpan.FromSeconds(2));
        cut.Find(".goal-grid-tree-inner .goal-grid-node-goal").Click();

        // Breadcrumb should appear with root goal name as link
        cut.WaitForState(() => cut.FindAll(".goal-grid-breadcrumb").Count > 0, TimeSpan.FromSeconds(2));
        cut.Find(".goal-grid-breadcrumb-link").TextContent.Should().Contain("Missão Raiz");
        cut.Find(".goal-grid-breadcrumb-current").TextContent.Should().Contain("Sub-Meta");
    }

    [Fact]
    public void Render_NavigateIntoEmpty_ShouldShowEmptyMessage()
    {
        var goal = CreateGoal("Meta Vazia");

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

        var cut = RenderComponent<GoalGridView>(parameters => parameters
            .Add(p => p.RootGoals, CreatePagedGoals(goal))
            .Add(p => p.RootGoalProgress, new Dictionary<Guid, GoalProgressResponse>()));

        cut.Find(".mission-card-expand-btn").Click();

        cut.WaitForState(() => cut.Markup.Contains("Nenhum indicador, tarefa ou meta nesta missão."), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Render_IndicatorCard_ShouldShowTargetLabel()
    {
        var goal = CreateGoal("Meta com Indicador");
        var indicator = CreateIndicator(goal.Id, "NPS Score");

        SetupServices(request =>
        {
            var url = request.RequestUri!.PathAndQuery;

            if (url.Contains($"/api/goals/{goal.Id}/children"))
            {
                return EmptyPagedResponse();
            }

            if (url.Contains($"/api/goals/{goal.Id}/indicators"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(SerializePagedResult<IndicatorResponse>([indicator]), Encoding.UTF8, "application/json")
                };
            }

            if (url.Contains($"/api/indicators/{indicator.Id}/progress"))
            {
                return JsonResponse(CreateIndicatorProgress(indicator.Id, 65));
            }

            return EmptyPagedResponse();
        });

        var cut = RenderComponent<GoalGridView>(parameters => parameters
            .Add(p => p.RootGoals, CreatePagedGoals(goal))
            .Add(p => p.RootGoalProgress, new Dictionary<Guid, GoalProgressResponse>()));

        // Navigate into goal to see its indicators
        cut.Find(".mission-card-expand-btn").Click();

        cut.WaitForState(() => cut.Markup.Contains("NPS Score"), TimeSpan.FromSeconds(2));
        cut.Markup.Should().Contain("65%");
        cut.FindAll(".goal-grid-node-indicator").Should().HaveCount(1);
    }

    [Fact]
    public void Render_EditButton_ShouldInvokeOnEdit()
    {
        SetupServices(_ => EmptyPagedResponse());

        var goal = CreateGoal("Meta Editável");
        GoalResponse? editedGoal = null;

        var cut = RenderComponent<GoalGridView>(parameters => parameters
            .Add(p => p.RootGoals, CreatePagedGoals(goal))
            .Add(p => p.RootGoalProgress, new Dictionary<Guid, GoalProgressResponse>())
            .Add(p => p.OnEdit, g => { editedGoal = g; }));

        // Open modal then click edit in modal header
        cut.Find(".mission-card-expand-btn").Click();
        cut.WaitForState(() => cut.FindAll(".goal-grid-container").Count > 0, TimeSpan.FromSeconds(2));

        var editBtn = cut.Find(".goal-grid-container .goal-grid-action-btn[title='Editar']");
        editBtn.Click();

        editedGoal.Should().NotBeNull();
        editedGoal!.Name.Should().Be("Meta Editável");
    }

    [Fact]
    public void Render_DeleteButton_ShouldInvokeOnDeleteClick()
    {
        SetupServices(_ => EmptyPagedResponse());

        var goal = CreateGoal("Meta Excluível");
        Guid? deletedId = null;

        var cut = RenderComponent<GoalGridView>(parameters => parameters
            .Add(p => p.RootGoals, CreatePagedGoals(goal))
            .Add(p => p.RootGoalProgress, new Dictionary<Guid, GoalProgressResponse>())
            .Add(p => p.OnDeleteClick, id => { deletedId = id; }));

        // Open modal then click delete in modal header
        cut.Find(".mission-card-expand-btn").Click();
        cut.WaitForState(() => cut.FindAll(".goal-grid-container").Count > 0, TimeSpan.FromSeconds(2));

        var deleteBtn = cut.Find(".goal-grid-container .goal-grid-action-btn[title='Excluir']");
        deleteBtn.Click();

        deletedId.Should().Be(goal.Id);
    }

    [Fact]
    public void Render_DeleteButton_ShouldShowConfirmationWhenDeletingGoalIdMatches()
    {
        SetupServices(_ => EmptyPagedResponse());

        var goal = CreateGoal("Meta Confirmar");

        var cut = RenderComponent<GoalGridView>(parameters => parameters
            .Add(p => p.RootGoals, CreatePagedGoals(goal))
            .Add(p => p.RootGoalProgress, new Dictionary<Guid, GoalProgressResponse>())
            .Add(p => p.DeletingGoalId, goal.Id));

        // Open modal — confirmation state is set via DeletingGoalId parameter
        cut.Find(".mission-card-expand-btn").Click();
        cut.WaitForState(() => cut.FindAll(".goal-grid-container").Count > 0, TimeSpan.FromSeconds(2));

        // Should show "OK?" confirmation text instead of trash icon
        cut.Find(".goal-grid-confirm-text").TextContent.Should().Be("OK?");
        // Button should have confirming class
        cut.Find(".goal-grid-action-btn.delete.confirming").Should().NotBeNull();
        // Title should change
        cut.Find("[title='Confirmar exclusão']").Should().NotBeNull();
    }

    [Fact]
    public void Render_IndicatorCheckinButton_ShouldInvokeOnCheckinClick()
    {
        var goal = CreateGoal("Meta Checkin");
        var indicator = CreateIndicator(goal.Id, "KPI Checkin");

        SetupServices(request =>
        {
            var url = request.RequestUri!.PathAndQuery;

            if (url.Contains($"/api/goals/{goal.Id}/children"))
            {
                return EmptyPagedResponse();
            }

            if (url.Contains($"/api/goals/{goal.Id}/indicators"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(SerializePagedResult<IndicatorResponse>([indicator]), Encoding.UTF8, "application/json")
                };
            }

            if (url.Contains($"/api/indicators/{indicator.Id}/progress"))
            {
                return JsonResponse(CreateIndicatorProgress(indicator.Id));
            }

            return EmptyPagedResponse();
        });

        (GoalResponse goal, IndicatorResponse indicator)? checkinArgs = null;

        var cut = RenderComponent<GoalGridView>(parameters => parameters
            .Add(p => p.RootGoals, CreatePagedGoals(goal))
            .Add(p => p.RootGoalProgress, new Dictionary<Guid, GoalProgressResponse>())
            .Add(p => p.OnCheckinClick, args => { checkinArgs = args; }));

        cut.Find(".mission-card-expand-btn").Click();
        cut.WaitForState(() => cut.Markup.Contains("KPI Checkin"), TimeSpan.FromSeconds(2));

        var checkinBtn = cut.Find(".goal-grid-node-indicator .goal-grid-action-btn[title='Novo check-in']");
        checkinBtn.Click();

        checkinArgs.Should().NotBeNull();
        checkinArgs!.Value.indicator.Name.Should().Be("KPI Checkin");
    }

    [Fact]
    public void Render_ClickIndicatorCard_ShouldInvokeOnHistoryClick()
    {
        var goal = CreateGoal("Meta Histórico");
        var indicator = CreateIndicator(goal.Id, "KPI Histórico");

        SetupServices(request =>
        {
            var url = request.RequestUri!.PathAndQuery;

            if (url.Contains($"/api/goals/{goal.Id}/children"))
            {
                return EmptyPagedResponse();
            }

            if (url.Contains($"/api/goals/{goal.Id}/indicators"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(SerializePagedResult<IndicatorResponse>([indicator]), Encoding.UTF8, "application/json")
                };
            }

            if (url.Contains($"/api/indicators/{indicator.Id}/progress"))
            {
                return JsonResponse(CreateIndicatorProgress(indicator.Id));
            }

            return EmptyPagedResponse();
        });

        (GoalResponse goal, IndicatorResponse indicator)? historyArgs = null;

        var cut = RenderComponent<GoalGridView>(parameters => parameters
            .Add(p => p.RootGoals, CreatePagedGoals(goal))
            .Add(p => p.RootGoalProgress, new Dictionary<Guid, GoalProgressResponse>())
            .Add(p => p.OnHistoryClick, args => { historyArgs = args; }));

        cut.Find(".mission-card-expand-btn").Click();
        cut.WaitForState(() => cut.Markup.Contains("KPI Histórico"), TimeSpan.FromSeconds(2));

        // Click the indicator card itself to open history
        cut.Find(".goal-grid-node-indicator").Click();

        historyArgs.Should().NotBeNull();
        historyArgs!.Value.indicator.Name.Should().Be("KPI Histórico");
    }

    [Fact]
    public void Render_WithNullRootGoals_ShouldRenderNothing()
    {
        SetupServices(_ => EmptyPagedResponse());

        var cut = RenderComponent<GoalGridView>(parameters => parameters
            .Add(p => p.RootGoals, null)
            .Add(p => p.RootGoalProgress, new Dictionary<Guid, GoalProgressResponse>()));

        cut.Markup.Trim().Should().BeEmpty();
    }

    [Fact]
    public void Render_WithEmptyRootGoals_ShouldRenderNothing()
    {
        SetupServices(_ => EmptyPagedResponse());

        var cut = RenderComponent<GoalGridView>(parameters => parameters
            .Add(p => p.RootGoals, CreatePagedGoals())
            .Add(p => p.RootGoalProgress, new Dictionary<Guid, GoalProgressResponse>()));

        cut.FindAll(".mission-card").Should().BeEmpty();
    }

    [Fact]
    public void Render_GoalCard_ShouldShowProgressBar()
    {
        SetupServices(_ => EmptyPagedResponse());

        var goal = CreateGoal("Meta Progresso");
        var progress = CreateGoalProgress(goal.Id, 55);

        var cut = RenderComponent<GoalGridView>(parameters => parameters
            .Add(p => p.RootGoals, CreatePagedGoals(goal))
            .Add(p => p.RootGoalProgress, new Dictionary<Guid, GoalProgressResponse> { [goal.Id] = progress }));

        cut.FindAll(".mission-card-progress-bar").Should().HaveCount(1);
        cut.FindAll(".mission-card-progress-fill").Should().HaveCount(1);
    }

    [Fact]
    public void Render_GoalCard_ShouldShowTitleInCard()
    {
        SetupServices(_ => EmptyPagedResponse());

        var goal = CreateGoal("Meta Label");

        var cut = RenderComponent<GoalGridView>(parameters => parameters
            .Add(p => p.RootGoals, CreatePagedGoals(goal))
            .Add(p => p.RootGoalProgress, new Dictionary<Guid, GoalProgressResponse>()));

        cut.Find(".mission-card-title").TextContent.Should().Be("Meta Label");
    }

    [Fact]
    public void Render_GoalCard_ShouldShowIndicatorCount()
    {
        SetupServices(_ => EmptyPagedResponse());

        var goal = CreateGoal("Meta Badge");
        var progress = CreateGoalProgress(goal.Id, 72);

        var cut = RenderComponent<GoalGridView>(parameters => parameters
            .Add(p => p.RootGoals, CreatePagedGoals(goal))
            .Add(p => p.RootGoalProgress, new Dictionary<Guid, GoalProgressResponse> { [goal.Id] = progress }));

        // TotalIndicators = 2 from CreateGoalProgress
        cut.Find(".mission-card-info").TextContent.Should().Contain("2");
    }

    [Fact]
    public void Render_IndicatorCard_ShouldShowIndicadorTypeLabel()
    {
        var goal = CreateGoal("Meta Tipo");
        var indicator = CreateIndicator(goal.Id, "KPI Tipo");

        SetupServices(request =>
        {
            var url = request.RequestUri!.PathAndQuery;

            if (url.Contains($"/api/goals/{goal.Id}/children"))
            {
                return EmptyPagedResponse();
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

        var cut = RenderComponent<GoalGridView>(parameters => parameters
            .Add(p => p.RootGoals, CreatePagedGoals(goal))
            .Add(p => p.RootGoalProgress, new Dictionary<Guid, GoalProgressResponse>()));

        cut.Find(".mission-card-expand-btn").Click();
        cut.WaitForState(() => cut.Markup.Contains("KPI Tipo"), TimeSpan.FromSeconds(2));

        var labels = cut.FindAll(".goal-grid-type-label");
        labels.Should().Contain(e => e.TextContent == "Indicador");
    }

    [Fact]
    public void Render_IndicatorCard_ShouldShowEditButton()
    {
        var goal = CreateGoal("Meta Edit Ind");
        var indicator = CreateIndicator(goal.Id, "KPI Editável");

        SetupServices(request =>
        {
            var url = request.RequestUri!.PathAndQuery;

            if (url.Contains($"/api/goals/{goal.Id}/children"))
            {
                return EmptyPagedResponse();
            }

            if (url.Contains($"/api/goals/{goal.Id}/indicators"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(SerializePagedResult<IndicatorResponse>([indicator]), Encoding.UTF8, "application/json")
                };
            }

            if (url.Contains($"/api/indicators/{indicator.Id}/progress"))
            {
                return JsonResponse(CreateIndicatorProgress(indicator.Id, 50));
            }

            return EmptyPagedResponse();
        });

        var cut = RenderComponent<GoalGridView>(parameters => parameters
            .Add(p => p.RootGoals, CreatePagedGoals(goal))
            .Add(p => p.RootGoalProgress, new Dictionary<Guid, GoalProgressResponse>()));

        cut.Find(".mission-card-expand-btn").Click();
        cut.WaitForState(() => cut.Markup.Contains("KPI Editável"), TimeSpan.FromSeconds(2));

        // Verify the indicator card has an edit button
        var indicatorEditBtns = cut.FindAll(".goal-grid-node-indicator [title='Editar']").ToList();
        indicatorEditBtns.Count.Should().Be(1);
    }

    [Fact]
    public void Render_IndicatorCard_ShouldShowIndicatorTypeInFooter()
    {
        var goal = CreateGoal("Meta Tipo Footer");
        var indicator = CreateIndicator(goal.Id, "KPI Quanti");

        SetupServices(request =>
        {
            var url = request.RequestUri!.PathAndQuery;

            if (url.Contains($"/api/goals/{goal.Id}/children"))
            {
                return EmptyPagedResponse();
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

        var cut = RenderComponent<GoalGridView>(parameters => parameters
            .Add(p => p.RootGoals, CreatePagedGoals(goal))
            .Add(p => p.RootGoalProgress, new Dictionary<Guid, GoalProgressResponse>()));

        cut.Find(".mission-card-expand-btn").Click();
        cut.WaitForState(() => cut.Markup.Contains("KPI Quanti"), TimeSpan.FromSeconds(2));

        cut.Find(".goal-grid-indicator-type").TextContent.Should().Be("Quantitativo");
    }

    [Fact]
    public void Render_GoalCard_ShouldShowIndicatorCountFromProgress()
    {
        SetupServices(_ => EmptyPagedResponse());

        var goal = CreateGoal("Meta Mista");
        var progress = CreateGoalProgress(goal.Id, 72, directChildren: 2, directIndicators: 3);

        var cut = RenderComponent<GoalGridView>(parameters => parameters
            .Add(p => p.RootGoals, CreatePagedGoals(goal))
            .Add(p => p.RootGoalProgress, new Dictionary<Guid, GoalProgressResponse> { [goal.Id] = progress }));

        // TotalIndicators = 2 from CreateGoalProgress; shown as "2 indicadores" in card info
        cut.Find(".mission-card-info").TextContent.Should().Contain("2");
        // Progress bar should be present
        cut.FindAll(".mission-card-progress-bar").Should().HaveCount(1);
    }

    [Fact]
    public void Render_ExpandButton_ShouldToggleExpandedState()
    {
        var parentGoal = CreateGoal("Missão Expandível");
        var childGoal = CreateGoal("Sub-Meta");

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
                return JsonResponse(new List<GoalProgressResponse> { CreateGoalProgress(childGoal.Id, 50) });
            }

            return EmptyPagedResponse();
        });

        var cut = RenderComponent<GoalGridView>(parameters => parameters
            .Add(p => p.RootGoals, CreatePagedGoals(parentGoal))
            .Add(p => p.RootGoalProgress, new Dictionary<Guid, GoalProgressResponse>
            {
                [parentGoal.Id] = CreateGoalProgress(parentGoal.Id)
            }));

        // Open goal container
        cut.Find(".mission-card-expand-btn").Click();
        cut.WaitForState(() => cut.FindAll(".goal-grid-container").Count > 0, TimeSpan.FromSeconds(2));

        // Container should not be expanded initially
        cut.FindAll(".goal-grid-container-expanded").Should().BeEmpty();
        cut.FindAll(".goal-grid-modal-backdrop").Should().HaveCount(1);

        // Click expand button
        cut.Find(".goal-grid-expand-btn").Click();

        // Container should now have expanded class
        cut.FindAll(".goal-grid-container-expanded").Should().HaveCount(1);

        // Click again to collapse
        cut.Find(".goal-grid-expand-btn").Click();

        // Should be collapsed again
        cut.FindAll(".goal-grid-container-expanded").Should().BeEmpty();
        cut.FindAll(".goal-grid-modal-backdrop").Should().HaveCount(1);
    }

    [Fact]
    public void Render_CloseExpandedContainer_ShouldResetExpandedState()
    {
        var parentGoal = CreateGoal("Missão Expandir Fechar");
        var childGoal = CreateGoal("Sub-Meta Fechar");

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
                return JsonResponse(new List<GoalProgressResponse> { CreateGoalProgress(childGoal.Id, 50) });
            }

            return EmptyPagedResponse();
        });

        var cut = RenderComponent<GoalGridView>(parameters => parameters
            .Add(p => p.RootGoals, CreatePagedGoals(parentGoal))
            .Add(p => p.RootGoalProgress, new Dictionary<Guid, GoalProgressResponse>
            {
                [parentGoal.Id] = CreateGoalProgress(parentGoal.Id)
            }));

        // Open and expand
        cut.Find(".mission-card-expand-btn").Click();
        cut.WaitForState(() => cut.FindAll(".goal-grid-container").Count > 0, TimeSpan.FromSeconds(2));
        cut.Find(".goal-grid-expand-btn").Click();
        cut.FindAll(".goal-grid-container-expanded").Should().HaveCount(1);

        // Close container
        cut.Find(".goal-grid-close-btn").Click();

        // Should return to root with no expanded state
        cut.WaitForState(() => cut.FindAll(".goal-grid-container").Count == 0, TimeSpan.FromSeconds(2));
        cut.FindAll(".goal-grid-container-expanded").Should().BeEmpty();
        cut.FindAll(".goal-grid-modal-backdrop").Should().BeEmpty();
    }

    private sealed class RouteHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}
