using Bud.Client.Services;
using Bud.Client.Shared.Goals;
using Bud.Shared.Contracts;

#pragma warning disable IDE0011, IDE0044, CA1822, CA1859, CA1860, CA1868

namespace Bud.Client.Pages;

public partial class Goals
{
    // Data
    private PagedResult<GoalResponse>? _goals;
    private Dictionary<Guid, GoalProgressResponse> _goalProgress = new();
    private List<OrganizationResponse> _organizations = new();
    private List<WorkspaceResponse> _workspaces = new();
    private List<TeamResponse> _teams = new();
    private List<CollaboratorResponse> _collaborators = new();

    // Filter state
    private string? _filterScopeTypeValue;
    private string? _filterScopeId;
    private string? _search;
    private string _viewMode = "list";
    private bool _showMyGoals = true;
    private bool _filterActiveOnly = true;
    private DateTime? _filterStartDate;
    private DateTime? _filterEndDate;

    // Card expansion state
    private HashSet<Guid> _expandedGoals = new();
    private Dictionary<Guid, List<IndicatorResponse>> _goalIndicatorsCache = new();
    private Dictionary<Guid, IndicatorProgressResponse> _indicatorProgressCache = new();
    private Dictionary<Guid, List<GoalResponse>> _goalChildrenCache = new();
    private Dictionary<Guid, GoalProgressResponse> _childGoalProgressCache = new();
    private HashSet<Guid> _expandedChildGoals = new();
    private int _cardRefreshToken;

    // Form modal state
    private bool _isWizardOpen;
    private bool _isEditMode;
    private Guid? _editingGoalId;
    private GoalFormModel? _wizardInitialModel;

    // Template picker state
    private bool _isTemplatePickerOpen;
    private List<TemplateResponse> _availableTemplates = new();

    // Checkin modal state
    private bool _isCheckinModalOpen;
    private GoalResponse? _checkinGoal;
    private IndicatorResponse? _selectedCheckinIndicator;

    // Checkin history modal state
    private bool _isCheckinHistoryModalOpen;
    private IndicatorResponse? _historyIndicator;
    private GoalResponse? _historyGoal;
    private List<CheckinResponse> _indicatorCheckins = new();
    private bool _isLoadingCheckins;

    // Delete confirmation state
    private Guid? _deletingGoalId;
    private System.Threading.Timer? _deleteConfirmTimer;

    protected override async Task OnInitializedAsync()
    {
        // Subscribe to organization changes
        OrgContext.OnOrganizationChanged += HandleOrganizationChanged;

        // Aguardar inicialização do OrgContext (MainLayout pode ainda estar carregando)
        var maxWait = 50; // 50 * 100ms = 5 segundos máximo
        while (!OrgContext.IsInitialized && maxWait > 0)
        {
            await Task.Delay(100);
            maxWait--;
        }

        await AuthState.EnsureInitializedAsync();
        await LoadReferenceData();
        await LoadGoals();
    }

    private void HandleOrganizationChanged()
    {
        _ = HandleOrganizationChangedAsync();
    }

    private async Task HandleOrganizationChangedAsync()
    {
        try
        {
            await InvokeAsync(async () =>
            {
                _filterScopeTypeValue = null;
                _filterScopeId = null;
                await LoadReferenceData();
                await LoadGoals();
                StateHasChanged();
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao atualizar missões por troca de organização: {ex.Message}");
            ToastService.ShowError("Erro ao atualizar missões", "Não foi possível atualizar os dados da organização selecionada.");
        }
    }

    public void Dispose()
    {
        OrgContext.OnOrganizationChanged -= HandleOrganizationChanged;
        _deleteConfirmTimer?.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task LoadReferenceData()
    {
        // Filtrar dados pelo tenant selecionado
        var orgId = OrgContext.SelectedOrganizationId;

        var orgsResult = await Api.GetOrganizationsAsync(null, 1, 100);
        _organizations = orgsResult?.Items.ToList() ?? new List<OrganizationResponse>();

        var workspacesResult = await Api.GetWorkspacesAsync(orgId, null, 1, 100);
        _workspaces = workspacesResult?.Items.ToList() ?? new List<WorkspaceResponse>();

        var teamsResult = await Api.GetTeamsAsync(null, null, null, 1, 100);
        _teams = teamsResult?.Items.ToList() ?? new List<TeamResponse>();

        var collaboratorsResult = await Api.GetCollaboratorsAsync(null, null, 1, 100);
        _collaborators = collaboratorsResult?.Items.ToList() ?? new List<CollaboratorResponse>();

    }

    private async Task SetGoalViewMode(bool myMissions)
    {
        if (_showMyGoals == myMissions)
        {
            return;
        }

        _showMyGoals = myMissions;

        if (_showMyGoals)
        {
            _filterScopeTypeValue = null;
            _filterScopeId = null;
        }

        await LoadGoals();
    }

    private async Task LoadGoals()
    {
        PagedResult<GoalResponse> result;

        if (_showMyGoals)
        {
            var collaboratorId = AuthState.SessionResponse?.CollaboratorId;
            if (collaboratorId.HasValue)
            {
                result = await Api.GetMyGoalsAsync(_search, 1, 100) ?? new PagedResult<GoalResponse>();
            }
            else
            {
                // Global admin without CollaboratorId — fallback to all goals
                result = await Api.GetGoalsAsync(null, null, _search, 1, 100) ?? new PagedResult<GoalResponse>();
            }
        }
        else
        {
            var scopeType = ParseScopeType(_filterScopeTypeValue);
            var scopeId = Guid.TryParse(_filterScopeId, out var parsedScopeId)
                ? parsedScopeId
                : (Guid?)null;

            result = await Api.GetGoalsAsync(scopeType, scopeId, _search, 1, 100) ?? new PagedResult<GoalResponse>();
        }

        // Apply client-side filters — only show root goals (children appear via expand/drilldown)
        var filteredItems = result.Items.Where(g => g.ParentId == null);

        // Filter by active status
        if (_filterActiveOnly)
        {
            filteredItems = filteredItems.Where(m => m.Status == GoalStatus.Active);
        }

        // Filter by date range
        if (_filterStartDate.HasValue)
        {
            filteredItems = filteredItems.Where(m => m.EndDate >= _filterStartDate.Value);
        }
        if (_filterEndDate.HasValue)
        {
            filteredItems = filteredItems.Where(m => m.StartDate <= _filterEndDate.Value);
        }

        var filteredList = filteredItems.ToList();
        _goals = new PagedResult<GoalResponse>
        {
            Items = filteredList,
            Total = filteredList.Count,
            Page = 1,
            PageSize = filteredList.Count
        };

        await LoadProgress();
    }

    private async Task LoadProgress()
    {
        if (_goals is null || _goals.Items.Count == 0)
        {
            _goalProgress = new();
            return;
        }

        try
        {
            var ids = _goals.Items.Select(m => m.Id).ToList();
            var progressList = await Api.GetGoalProgressAsync(ids);
            _goalProgress = progressList?.ToDictionary(p => p.GoalId) ?? new();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao carregar progresso das metas: {ex.Message}");
            _goalProgress = new();
        }
    }

    private async Task LoadIndicatorProgress(Guid goalId)
    {
        if (!_goalIndicatorsCache.TryGetValue(goalId, out var indicators) || indicators.Count == 0)
            return;

        try
        {
            var indicatorIds = indicators.Select(m => m.Id).ToList();
            var progressList = await Api.GetIndicatorProgressAsync(indicatorIds);
            if (progressList != null)
            {
                foreach (var progress in progressList)
                {
                    _indicatorProgressCache[progress.IndicatorId] = progress;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao carregar progresso dos indicadores: {ex.Message}");
        }
    }



    private void HandleToggleViewMode()
    {
        _viewMode = _viewMode == "list" ? "grid" : "list";
    }

    private async Task HandleToggleActiveFilter()
    {
        _filterActiveOnly = !_filterActiveOnly;
        await LoadGoals();
    }

    private async Task HandleDateFilterApplied((DateTime?, DateTime?) dates)
    {
        _filterStartDate = dates.Item1;
        _filterEndDate = dates.Item2;
        await LoadGoals();
    }

    private async Task HandleFilterScopeTypeChanged(string? value)
    {
        _filterScopeTypeValue = value;
        _filterScopeId = null;
        await LoadGoals();
    }

    private async Task HandleFilterScopeIdChanged(string? value)
    {
        _filterScopeId = value;
        await LoadGoals();
    }

    private async Task HandleSearchSubmit(string? value)
    {
        _search = value;
        await LoadGoals();
    }

    private async Task HandleClearFilters()
    {
        _filterScopeTypeValue = null;
        _filterScopeId = null;
        _search = null;
        _filterActiveOnly = true;
        _filterStartDate = null;
        _filterEndDate = null;
        await LoadGoals();
    }

    private async Task OpenCreateModal()
    {
        // Load available templates and open the picker
        try
        {
            var result = await Api.GetTemplatesAsync(null, 1, 50);
            _availableTemplates = result?.Items.ToList() ?? new();
        }
        catch
        {
            _availableTemplates = new();
        }

        _isTemplatePickerOpen = true;
    }

    private void OpenCreateModalFromScratch()
    {
        _isTemplatePickerOpen = false;
        _wizardInitialModel = new GoalFormModel
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(7)
        };
        _isEditMode = false;
        _editingGoalId = null;
        _isWizardOpen = true;
    }

    private void OpenCreateModalFromTemplate(TemplateResponse template)
    {
        _isTemplatePickerOpen = false;

        // Build hierarchical model: indicators belong to their goals
        var directIndicators = template.Indicators
            .Where(m => !m.TemplateGoalId.HasValue)
            .OrderBy(m => m.OrderIndex)
            .Select(m => BuildTempIndicatorFromTemplate(m))
            .ToList();

        var children = template.Goals
            .OrderBy(o => o.OrderIndex)
            .Select(o =>
            {
                var goalIndicators = template.Indicators
                    .Where(m => m.TemplateGoalId == o.Id)
                    .OrderBy(m => m.OrderIndex)
                    .Select(m => BuildTempIndicatorFromTemplate(m))
                    .ToList();

                return new TempGoal(
                    Guid.NewGuid().ToString(),
                    o.Name,
                    o.Description,
                    null,
                    o.Dimension)
                {
                    Indicators = goalIndicators
                };
            })
            .ToList();

        _wizardInitialModel = new GoalFormModel
        {
            Name = template.GoalNamePattern,
            Description = template.GoalDescriptionPattern,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(90),
            Children = children,
            Indicators = directIndicators
        };
        _isEditMode = false;
        _editingGoalId = null;
        _isWizardOpen = true;
    }

    private static TempIndicator BuildTempIndicatorFromTemplate(TemplateIndicatorResponse m)
    {
        return new TempIndicator(
            null, m.Name, m.Type.ToString(), GetTemplateIndicatorDetails(m),
            m.QuantitativeType?.ToString(), m.MinValue, m.MaxValue, m.TargetText, m.Unit?.ToString());
    }

    private static string GetTemplateIndicatorDetails(TemplateIndicatorResponse indicator)
    {
        if (indicator.Type == IndicatorType.Qualitative)
            return $"Qualitativa — {indicator.TargetText}";

        var parts = new List<string> { "Quantitativa" };
        if (indicator.QuantitativeType.HasValue)
            parts.Add(indicator.QuantitativeType.Value.ToString());
        if (indicator.Unit.HasValue)
            parts.Add(indicator.Unit.Value.ToString());
        if (indicator.MinValue.HasValue)
            parts.Add($"Min: {indicator.MinValue}");
        if (indicator.MaxValue.HasValue)
            parts.Add($"Max: {indicator.MaxValue}");
        return string.Join(" — ", parts);
    }

    private void CloseWizard()
    {
        _isWizardOpen = false;
        _isEditMode = false;
        _editingGoalId = null;
        _wizardInitialModel = null;
    }

    private string GetSelectedOrganizationName()
    {
        if (OrgContext.SelectedOrganizationId.HasValue)
        {
            var selectedOrganization = _organizations.FirstOrDefault(o => o.Id == OrgContext.SelectedOrganizationId.Value);
            if (selectedOrganization is not null)
            {
                return selectedOrganization.Name;
            }
        }

        return _organizations.FirstOrDefault()?.Name ?? "Organização";
    }

    // ---- Wizard Handlers ----

    private async Task HandleWizardSave(GoalFormResult result)
    {
        if (_isEditMode && _editingGoalId.HasValue)
        {
            await UpdateGoalFromResult(_editingGoalId.Value, result);
        }
        else
        {
            await CreateGoalFromResult(result, GoalStatus.Active,
                "Missão criada com sucesso!",
                m => $"A missão '{m?.Name}' foi criada e está ativa.",
                "Erro ao criar missão",
                "Não foi possível criar a missão. Verifique os dados e tente novamente.");
        }
    }

    private async Task HandleWizardSaveDraft(GoalFormResult result)
    {
        await CreateGoalFromResult(result, GoalStatus.Planned,
            "Rascunho salvo com sucesso!",
            _ => "A missão foi salva como rascunho.",
            "Erro ao salvar rascunho",
            "Não foi possível salvar o rascunho da missão. Verifique os dados e tente novamente.");
    }

    private async Task CreateGoalFromResult(
        GoalFormResult result,
        GoalStatus status,
        string successTitle,
        Func<GoalResponse?, string> successMessageFactory,
        string errorTitle,
        string errorMessage)
    {
        if (!Enum.TryParse<GoalScopeType>(result.ScopeTypeValue, out var scopeType)) return;
        if (!Guid.TryParse(result.ScopeId, out var scopeId)) return;

        await UiOps.RunAsync(
            async () =>
            {
                var request = new CreateGoalRequest
                {
                    Name = result.Name,
                    Description = result.Description,
                    StartDate = result.StartDate,
                    EndDate = result.EndDate,
                    ScopeType = scopeType,
                    ScopeId = scopeId,
                    Status = status
                };

                var createdGoal = await Api.CreateGoalAsync(request);
                if (createdGoal is not null)
                {
                    var failedCount = 0;
                    failedCount += await CreateIndicatorsForGoalAsync(createdGoal.Id, result.Indicators);
                    failedCount += await CreateChildrenRecursiveAsync(createdGoal, result.Children);
                    ShowPartialFailureIfAny(failedCount);
                }

                await LoadGoals();
                ToastService.ShowSuccess(successTitle, successMessageFactory(createdGoal));
                CloseWizard();
            },
            errorTitle,
            errorMessage);
    }

    private async Task<int> CreateChildrenRecursiveAsync(GoalResponse parentGoal, List<TempGoal> children)
    {
        var failedCount = 0;
        var (parentScopeType, parentScopeId) = GetGoalScope(parentGoal);

        foreach (var child in children)
        {
            try
            {
                var childScopeType = Enum.TryParse<GoalScopeType>(child.ScopeTypeValue, out var cst) ? cst : parentScopeType;
                var childScopeId = Guid.TryParse(child.ScopeId, out var csi) ? csi : parentScopeId;
                var childStatus = Enum.TryParse<GoalStatus>(child.StatusValue, out var cs) ? cs : parentGoal.Status;

                var created = await Api.CreateChildGoalAsync(new CreateGoalRequest
                {
                    ParentId = parentGoal.Id,
                    Name = child.Name,
                    Description = child.Description,
                    Dimension = child.Dimension,
                    StartDate = child.StartDate ?? parentGoal.StartDate,
                    EndDate = child.EndDate ?? parentGoal.EndDate,
                    Status = childStatus,
                    ScopeType = childScopeType,
                    ScopeId = childScopeId
                });
                if (created is not null)
                {
                    failedCount += await CreateIndicatorsForGoalAsync(created.Id, child.Indicators);
                    failedCount += await CreateChildrenRecursiveAsync(created, child.Children);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erro ao criar meta '{child.Name}' para missão {parentGoal.Id}: {ex.Message}");
                failedCount++;
            }
        }

        return failedCount;
    }

    private async Task<int> CreateIndicatorsForGoalAsync(Guid goalId, List<TempIndicator> indicators)
    {
        var failedCount = 0;
        foreach (var tempIndicator in indicators)
        {
            var request = BuildCreateIndicatorRequest(goalId, tempIndicator);
            if (request is null)
            {
                failedCount++;
                continue;
            }

            try
            {
                await Api.CreateGoalIndicatorAsync(request);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erro ao criar indicador '{tempIndicator.Name}' para meta {goalId}: {ex.Message}");
                failedCount++;
            }
        }

        return failedCount;
    }

    private void ShowPartialFailureIfAny(int failedCount)
    {
        if (failedCount > 0)
        {
            ToastService.ShowError("Erro ao salvar", $"{failedCount} item(ns) não puderam ser salvos.");
        }
    }

    private IEnumerable<ScopeOption> GetScopeOptions(string? scopeTypeValue)
    {
        if (!Enum.TryParse<GoalScopeType>(scopeTypeValue, out var scopeType))
        {
            return Enumerable.Empty<ScopeOption>();
        }

        return scopeType switch
        {
            GoalScopeType.Organization => _organizations.Select(o => new ScopeOption(o.Id.ToString(), o.Name)),
            GoalScopeType.Workspace => _workspaces.Select(w => new ScopeOption(w.Id.ToString(), w.Name)),
            GoalScopeType.Team => _teams.Select(t => new ScopeOption(t.Id.ToString(), t.Name)),
            GoalScopeType.Collaborator => _collaborators.Select(c => new ScopeOption(c.Id.ToString(), c.FullName)),
            _ => Enumerable.Empty<ScopeOption>()
        };
    }

    private GoalScopeType? ParseScopeType(string? scopeTypeValue)
    {
        return Enum.TryParse<GoalScopeType>(scopeTypeValue, out var scopeType) ? scopeType : null;
    }


    // ---- Card Expansion Methods ----

    private async Task ToggleExpand(Guid goalId)
    {
        if (_expandedGoals.Contains(goalId))
        {
            _expandedGoals.Remove(goalId);
            return;
        }

        await ExpandGoalAsync(goalId);
    }

    private async Task ExpandGoalAsync(Guid goalId)
    {
        _expandedGoals.Add(goalId);
        if (_goalIndicatorsCache.ContainsKey(goalId))
        {
            return;
        }

        var (indicators, childGoals) = await LoadGoalDetailsIntoCacheAsync(goalId);
        await LoadCardProgressAsync(indicators, childGoals);
        StateHasChanged();
    }

    private async Task<(List<IndicatorResponse> indicators, List<GoalResponse> childGoals)> LoadGoalDetailsIntoCacheAsync(Guid goalId)
    {
        var indicatorsTask = Api.GetGoalIndicatorsByGoalIdAsync(goalId);
        var childGoalsTask = Api.GetGoalChildrenAsync(goalId);
        await Task.WhenAll(indicatorsTask, childGoalsTask);

        var indicators = indicatorsTask.Result?.Items.ToList() ?? [];
        var childGoals = childGoalsTask.Result?.Items.ToList() ?? [];

        _goalIndicatorsCache[goalId] = indicators;
        _goalChildrenCache[goalId] = childGoals;

        return (indicators, childGoals);
    }

    private async Task LoadCardProgressAsync(List<IndicatorResponse> indicators, List<GoalResponse> childGoals)
    {
        var indicatorProgressTask = indicators.Count > 0
            ? LoadIndicatorProgressForIds(indicators.Select(m => m.Id).ToList())
            : Task.CompletedTask;

        var childGoalProgressTask = childGoals.Count > 0
            ? LoadChildGoalProgress(childGoals.Select(o => o.Id).ToList())
            : Task.CompletedTask;

        await Task.WhenAll(indicatorProgressTask, childGoalProgressTask);
    }

    private async Task LoadChildGoalProgress(List<Guid> childGoalIds)
    {
        var progressList = await Api.GetGoalProgressAsync(childGoalIds);
        if (progressList != null)
        {
            foreach (var progress in progressList)
            {
                _childGoalProgressCache[progress.GoalId] = progress;
            }
        }
    }

    private async Task LoadIndicatorProgressForIds(List<Guid> indicatorIds)
    {
        var progressList = await Api.GetIndicatorProgressAsync(indicatorIds);
        if (progressList != null)
        {
            foreach (var progress in progressList)
            {
                _indicatorProgressCache[progress.IndicatorId] = progress;
            }
        }
    }

    // ---- Checkin History Modal Methods ----

    private async Task OpenCheckinHistoryModal(GoalResponse goal, IndicatorResponse indicator)
    {
        _historyGoal = goal;
        _historyIndicator = indicator;
        _isCheckinHistoryModalOpen = true;
        _isLoadingCheckins = true;
        _indicatorCheckins = new List<CheckinResponse>();
        StateHasChanged();

        try
        {
            var result = await Api.GetCheckinsByIndicatorAsync(indicator.Id, 1, 50);
            _indicatorCheckins = result?.Items.OrderByDescending(c => c.CheckinDate).ToList() ?? new List<CheckinResponse>();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao carregar histórico de check-ins do indicador {indicator.Id}: {ex.Message}");
            _indicatorCheckins = new List<CheckinResponse>();
            ToastService.ShowError("Erro ao carregar histórico", "Não foi possível carregar o histórico de check-ins deste indicador.");
        }
        finally
        {
            _isLoadingCheckins = false;
            StateHasChanged();
        }
    }

    private void CloseCheckinHistoryModal()
    {
        _isCheckinHistoryModalOpen = false;
        _historyIndicator = null;
        _historyGoal = null;
        _indicatorCheckins = new List<CheckinResponse>();
    }

    private void OpenCheckinFromHistory()
    {
        // Capture values before closing the history modal
        var goal = _historyGoal;
        var indicator = _historyIndicator;

        // Close history modal first
        _isCheckinHistoryModalOpen = false;
        _historyIndicator = null;
        _historyGoal = null;
        _indicatorCheckins = new List<CheckinResponse>();

        // Now open checkin modal with captured values
        if (goal != null && indicator != null)
        {
            OpenCheckinModalForIndicator(goal, indicator);
        }
    }

    // ---- Summary Card Methods ----

    private int GetOverallProgressDisplay()
    {
        if (_goals is null || !_goalProgress.Any()) return 0;
        var activeProgress = _goalProgress.Values.Where(p => p.TotalIndicators > 0).ToList();
        if (!activeProgress.Any()) return 0;
        return (int)activeProgress.Average(p => p.OverallProgress);
    }

    private int GetExpectedProgressDisplay()
    {
        if (_goals is null || !_goalProgress.Any()) return 0;
        var activeProgress = _goalProgress.Values.Where(p => p.ExpectedProgress > 0).ToList();
        if (!activeProgress.Any()) return 0;
        return (int)activeProgress.Average(p => p.ExpectedProgress);
    }

    private int GetActiveGoalsCount()
    {
        return _goals?.Items.Count(m => m.Status == GoalStatus.Active) ?? 0;
    }

    private int GetOutdatedIndicatorsCount()
    {
        return _goalProgress.Values.Sum(p => p.OutdatedIndicators);
    }

    // ---- Checkin Modal Methods ----

    private void OpenCheckinModalForIndicator(GoalResponse goal, IndicatorResponse indicator)
    {
        _checkinGoal = goal;
        _selectedCheckinIndicator = indicator;
        _isCheckinModalOpen = true;
    }

    private void CloseCheckinModal()
    {
        _isCheckinModalOpen = false;
        _checkinGoal = null;
        _selectedCheckinIndicator = null;
    }

    private async Task HandleCheckinSubmit(CreateCheckinRequest request)
    {
        if (request.ConfidenceLevel < 1 || request.ConfidenceLevel > 5)
        {
            ToastService.ShowError("Erro ao criar check-in", "Selecione o nível de confiança (1-5).");
            return;
        }

        await UiOps.RunAsync(
            async () =>
            {
                await Api.CreateCheckinAsync(_selectedCheckinIndicator!.Id, request);
                ToastService.ShowSuccess("Check-in criado", "O check-in foi registrado com sucesso.");
                var goalId = _checkinGoal?.Id;
                CloseCheckinModal();
                await RefreshExpandedGoalProgressAsync(goalId);
            },
            "Erro ao criar check-in",
            "Não foi possível criar o check-in. Verifique os dados e tente novamente.");
    }

    private async Task RefreshExpandedGoalProgressAsync(Guid? checkinGoalId)
    {
        await LoadProgress();

        // Find the root goal that is expanded and contains this checkin goal
        var expandedRootIds = _expandedGoals.ToList();
        foreach (var rootId in expandedRootIds)
        {
            // Invalidate caches so card re-fetches data (including child GoalChildSections)
            _goalIndicatorsCache.Remove(rootId);
            _goalChildrenCache.Remove(rootId);

            var (indicators, childGoals) = await LoadGoalDetailsIntoCacheAsync(rootId);
            await LoadCardProgressAsync(indicators, childGoals);
        }

        // Also refresh indicator progress for the direct checkin goal if cached
        if (checkinGoalId.HasValue && _goalIndicatorsCache.ContainsKey(checkinGoalId.Value))
        {
            await LoadIndicatorProgress(checkinGoalId.Value);
        }

        // Force GoalChildSection components to re-mount and reload their internal state
        _cardRefreshToken++;
    }

    // ---- Goal Edit/Delete Methods ----

    private (GoalScopeType scopeType, Guid scopeId) GetGoalScope(GoalResponse goal)
    {
        if (goal.CollaboratorId.HasValue)
            return (GoalScopeType.Collaborator, goal.CollaboratorId.Value);
        if (goal.TeamId.HasValue)
            return (GoalScopeType.Team, goal.TeamId.Value);
        if (goal.WorkspaceId.HasValue)
            return (GoalScopeType.Workspace, goal.WorkspaceId.Value);
        return (GoalScopeType.Organization, goal.OrganizationId);
    }

    private async Task OpenEditWizard(GoalResponse goal)
    {
        _isEditMode = true;
        _editingGoalId = goal.Id;

        var (scopeType, scopeId) = GetGoalScope(goal);

        var (directIndicators, tempGoals) = await LoadGoalChildrenForEditAsync(goal.Id);

        _wizardInitialModel = new GoalFormModel
        {
            Name = goal.Name,
            Description = goal.Description,
            StartDate = goal.StartDate,
            EndDate = goal.EndDate,
            ScopeTypeValue = scopeType.ToString(),
            ScopeId = scopeId.ToString(),
            StatusValue = goal.Status.ToString(),
            Indicators = directIndicators,
            Children = tempGoals
        };

        _isWizardOpen = true;
    }

    private async Task<(List<TempIndicator> directIndicators, List<TempGoal> children)> LoadGoalChildrenForEditAsync(Guid goalId)
    {
        var indicatorsTask = Api.GetGoalIndicatorsByGoalIdAsync(goalId);
        var childGoalsTask = Api.GetGoalChildrenAsync(goalId);
        await Task.WhenAll(indicatorsTask, childGoalsTask);

        var apiIndicators = indicatorsTask.Result?.Items.ToList() ?? new List<IndicatorResponse>();
        var apiChildGoals = childGoalsTask.Result?.Items.ToList() ?? new List<GoalResponse>();

        var childGoalIds = apiChildGoals.Select(o => o.Id).ToHashSet();
        var directIndicators = apiIndicators
            .Where(m => !childGoalIds.Contains(m.GoalId))
            .Select(BuildTempIndicatorFromApi)
            .ToList();

        var tempGoals = new List<TempGoal>();
        foreach (var childGoal in apiChildGoals)
        {
            var (childScopeType, childScopeId) = GetGoalScope(childGoal);

            // Recursively load child's own indicators and grandchildren
            var (childIndicators, grandChildren) = await LoadGoalChildrenForEditAsync(childGoal.Id);

            tempGoals.Add(new TempGoal(
                TempId: Guid.NewGuid().ToString(),
                Name: childGoal.Name,
                Description: childGoal.Description,
                OriginalId: childGoal.Id,
                Dimension: childGoal.Dimension,
                StartDate: childGoal.StartDate,
                EndDate: childGoal.EndDate,
                ScopeTypeValue: childScopeType.ToString(),
                ScopeId: childScopeId.ToString(),
                StatusValue: childGoal.Status.ToString())
            {
                Indicators = childIndicators,
                Children = grandChildren
            });
        }

        return (directIndicators, tempGoals);
    }

    private static TempIndicator BuildTempIndicatorFromApi(IndicatorResponse m)
    {
        return new TempIndicator(
            OriginalId: m.Id,
            Name: m.Name,
            Type: m.Type.ToString(),
            Details: GetIndicatorDetailsFromModel(m),
            QuantitativeType: m.QuantitativeType?.ToString(),
            MinValue: m.MinValue,
            MaxValue: m.MaxValue,
            TargetText: m.TargetText,
            Unit: m.Unit?.ToString());
    }

    private async Task UpdateGoalFromResult(Guid goalId, GoalFormResult result)
    {
        if (!Enum.TryParse<GoalScopeType>(result.ScopeTypeValue, out var scopeType)) return;
        if (!Guid.TryParse(result.ScopeId, out var scopeId)) return;

        await UiOps.RunAsync(
            async () =>
            {
                await Api.UpdateGoalAsync(goalId, BuildUpdateGoalRequest(result, scopeType, scopeId));

                // Delete removed items first
                var failureCount = 0;
                failureCount += await DeleteRemovedItemsAsync(result);

                // Process direct indicators (update existing, create new)
                failureCount += await ProcessIndicatorChangesForGoalAsync(goalId, result.Indicators);

                // Process child goals recursively
                failureCount += await ProcessChildGoalChangesRecursiveAsync(goalId, result);

                ShowPartialFailureIfAny(failureCount);
                await RefreshGoalCachesAsync(goalId);

                ToastService.ShowSuccess("Missão atualizada", "As alterações foram salvas com sucesso.");
                CloseWizard();
            },
            "Erro ao atualizar",
            "Não foi possível atualizar a missão. Verifique os dados e tente novamente.");
    }

    private static PatchGoalRequest BuildUpdateGoalRequest(GoalFormResult result, GoalScopeType scopeType, Guid scopeId)
    {
        var request = new PatchGoalRequest
        {
            Name = result.Name,
            Description = result.Description,
            StartDate = result.StartDate,
            EndDate = result.EndDate,
            ScopeType = scopeType,
            ScopeId = scopeId
        };

        if (Enum.TryParse<GoalStatus>(result.StatusValue, out var status))
        {
            request.Status = status;
        }

        return request;
    }

    private async Task<int> DeleteRemovedItemsAsync(GoalFormResult result)
    {
        var failureCount = 0;

        foreach (var indicatorId in result.DeletedIndicatorIds)
        {
            try
            {
                await Api.DeleteGoalIndicatorAsync(indicatorId);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erro ao excluir indicador {indicatorId}: {ex.Message}");
                failureCount++;
            }
        }

        foreach (var goalId in result.DeletedGoalIds)
        {
            try
            {
                await Api.DeleteGoalAsync(goalId);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erro ao excluir meta {goalId}: {ex.Message}");
                failureCount++;
            }
        }

        return failureCount;
    }

    private async Task<int> ProcessIndicatorChangesForGoalAsync(Guid goalId, List<TempIndicator> indicators)
    {
        var failureCount = 0;

        foreach (var indicator in indicators.Where(m => m.OriginalId.HasValue))
        {
            try
            {
                var request = BuildUpdateIndicatorRequest(indicator);
                if (request is not null)
                {
                    await Api.UpdateGoalIndicatorAsync(indicator.OriginalId!.Value, request);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erro ao atualizar indicador {indicator.OriginalId}: {ex.Message}");
                failureCount++;
            }
        }

        foreach (var indicator in indicators.Where(m => !m.OriginalId.HasValue))
        {
            try
            {
                var request = BuildCreateIndicatorRequest(goalId, indicator);
                if (request is null)
                {
                    failureCount++;
                    continue;
                }

                await Api.CreateGoalIndicatorAsync(request);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erro ao criar indicador '{indicator.Name}': {ex.Message}");
                failureCount++;
            }
        }

        return failureCount;
    }

    private async Task<int> ProcessChildGoalChangesRecursiveAsync(Guid parentGoalId, GoalFormResult result)
    {
        var failureCount = 0;

        // Update existing child goals
        foreach (var child in result.Children.Where(o => o.OriginalId.HasValue))
        {
            try
            {
                var patchRequest = new PatchGoalRequest
                {
                    Name = child.Name,
                    Description = child.Description,
                    Dimension = child.Dimension
                };

                if (child.StartDate.HasValue) patchRequest.StartDate = child.StartDate.Value;
                if (child.EndDate.HasValue) patchRequest.EndDate = child.EndDate.Value;
                if (Enum.TryParse<GoalScopeType>(child.ScopeTypeValue, out var st)) patchRequest.ScopeType = st;
                if (Guid.TryParse(child.ScopeId, out var si)) patchRequest.ScopeId = si;
                if (Enum.TryParse<GoalStatus>(child.StatusValue, out var sv)) patchRequest.Status = sv;

                await Api.UpdateGoalAsync(child.OriginalId!.Value, patchRequest);

                // Process this child's indicators
                failureCount += await ProcessIndicatorChangesForGoalAsync(child.OriginalId.Value, child.Indicators);

                // Recursively process grandchildren
                var childResult = new GoalFormResult
                {
                    Name = child.Name,
                    Description = child.Description,
                    StartDate = child.StartDate ?? result.StartDate,
                    EndDate = child.EndDate ?? result.EndDate,
                    ScopeTypeValue = child.ScopeTypeValue ?? result.ScopeTypeValue,
                    ScopeId = child.ScopeId ?? result.ScopeId,
                    StatusValue = child.StatusValue ?? result.StatusValue,
                    Indicators = child.Indicators,
                    Children = child.Children,
                    DeletedIndicatorIds = result.DeletedIndicatorIds,
                    DeletedGoalIds = result.DeletedGoalIds
                };
                failureCount += await ProcessChildGoalChangesRecursiveAsync(child.OriginalId.Value, childResult);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erro ao atualizar meta {child.OriginalId}: {ex.Message}");
                failureCount++;
            }
        }

        // Create new child goals recursively
        foreach (var child in result.Children.Where(o => !o.OriginalId.HasValue))
        {
            try
            {
                // Resolve scope: child's own or fallback to parent's
                if (!Enum.TryParse<GoalScopeType>(child.ScopeTypeValue, out var childScopeType))
                {
                    if (!Enum.TryParse(result.ScopeTypeValue, out childScopeType))
                    {
                        failureCount++;
                        continue;
                    }
                }

                var childScopeId = Guid.TryParse(child.ScopeId, out var csi) ? csi : Guid.Empty;
                if (childScopeId == Guid.Empty)
                {
                    if (!Guid.TryParse(result.ScopeId, out childScopeId))
                    {
                        failureCount++;
                        continue;
                    }
                }

                if (!Enum.TryParse<GoalStatus>(child.StatusValue, out var childGoalStatus))
                    _ = Enum.TryParse(result.StatusValue, out childGoalStatus);

                var created = await Api.CreateChildGoalAsync(new CreateGoalRequest
                {
                    ParentId = parentGoalId,
                    Name = child.Name,
                    Description = child.Description,
                    Dimension = child.Dimension,
                    StartDate = child.StartDate ?? result.StartDate,
                    EndDate = child.EndDate ?? result.EndDate,
                    Status = childGoalStatus,
                    ScopeType = childScopeType,
                    ScopeId = childScopeId
                });

                if (created is not null)
                {
                    failureCount += await CreateIndicatorsForGoalAsync(created.Id, child.Indicators);
                    failureCount += await CreateChildrenRecursiveAsync(created, child.Children);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erro ao criar meta '{child.Name}': {ex.Message}");
                failureCount++;
            }
        }

        return failureCount;
    }

    private async Task RefreshGoalCachesAsync(Guid goalId)
    {
        _goalIndicatorsCache.Remove(goalId);
        _goalChildrenCache.Remove(goalId);
        await LoadGoals();

        if (_expandedGoals.Contains(goalId))
        {
            var (indicators, childGoals) = await LoadGoalDetailsIntoCacheAsync(goalId);
            if (indicators.Count > 0)
            {
                await LoadIndicatorProgress(goalId);
            }

            if (childGoals.Count > 0)
            {
                await LoadChildGoalProgress(childGoals.Select(o => o.Id).ToList());
            }
        }

        // Force GoalChildSection components to re-mount and reload their internal state
        _cardRefreshToken++;
    }

    private async Task HandleDeleteClick(Guid goalId)
    {
        if (_deletingGoalId == goalId)
        {
            await DeleteGoal(goalId);
        }
        else
        {
            ArmGoalDeleteConfirmation(goalId);
        }
    }

    private async Task DeleteGoal(Guid goalId)
    {
        try
        {
            await UiOps.RunAsync(
                async () =>
                {
                    await Api.DeleteGoalAsync(goalId);
                    ToastService.ShowSuccess("Missão excluída", "A missão foi removida com sucesso.");
                    await LoadGoals();
                },
            "Erro ao excluir",
            "Não foi possível excluir a missão. Tente novamente.");
        }
        finally
        {
            ClearGoalDeleteConfirmation();
        }
    }

    private static string GetIndicatorDetailsFromModel(IndicatorResponse m)
    {
        if (m.Type == IndicatorType.Qualitative)
            return m.TargetText ?? "";

        var unitLabel = m.Unit?.ToString() switch
        {
            "Integer" or "Decimal" => "un",
            "Percentage" => "%",
            "Hours" => "h",
            "Points" => "pts",
            _ => ""
        };
        return m.QuantitativeType?.ToString() switch
        {
            "KeepAbove" => $"Acima de {m.MinValue} {unitLabel}",
            "KeepBelow" => $"Abaixo de {m.MaxValue} {unitLabel}",
            "KeepBetween" => $"Entre {m.MinValue} e {m.MaxValue} {unitLabel}",
            "Achieve" => $"Atingir {m.MaxValue} {unitLabel}",
            "Reduce" => $"Reduzir para {m.MaxValue} {unitLabel}",
            _ => ""
        };
    }

    private CreateIndicatorRequest? BuildCreateIndicatorRequest(Guid goalId, TempIndicator indicator)
    {
        if (!EnumParsingHelper.TryParseEnum<IndicatorType>(indicator.Type, out var indicatorType))
            return null;

        var request = new CreateIndicatorRequest
        {
            GoalId = goalId,
            Name = indicator.Name,
            Type = indicatorType,
            MinValue = indicator.MinValue,
            MaxValue = indicator.MaxValue,
            TargetText = indicator.TargetText
        };

        if (indicatorType == IndicatorType.Quantitative)
        {
            if (!string.IsNullOrEmpty(indicator.QuantitativeType) &&
                EnumParsingHelper.TryParseEnum<QuantitativeIndicatorType>(indicator.QuantitativeType, out var qType))
                request.QuantitativeType = qType;
            if (!string.IsNullOrEmpty(indicator.Unit) &&
                EnumParsingHelper.TryParseEnum<IndicatorUnit>(indicator.Unit, out var unit))
                request.Unit = unit;
        }

        return request;
    }

    private PatchIndicatorRequest? BuildUpdateIndicatorRequest(TempIndicator indicator)
    {
        if (!EnumParsingHelper.TryParseEnum<IndicatorType>(indicator.Type, out var indicatorType))
            return null;

        var request = new PatchIndicatorRequest
        {
            Name = indicator.Name,
            Type = indicatorType,
            MinValue = indicator.MinValue,
            MaxValue = indicator.MaxValue,
            TargetText = indicator.TargetText
        };

        if (indicatorType == IndicatorType.Quantitative)
        {
            if (!string.IsNullOrEmpty(indicator.QuantitativeType) &&
                EnumParsingHelper.TryParseEnum<QuantitativeIndicatorType>(indicator.QuantitativeType, out var qType))
                request.QuantitativeType = qType;
            if (!string.IsNullOrEmpty(indicator.Unit) &&
                EnumParsingHelper.TryParseEnum<IndicatorUnit>(indicator.Unit, out var unit))
                request.Unit = unit;
        }

        return request;
    }

    private void ArmGoalDeleteConfirmation(Guid goalId)
    {
        _deletingGoalId = goalId;
        _deleteConfirmTimer?.Dispose();
        _deleteConfirmTimer = new System.Threading.Timer(
            _ => InvokeAsync(() =>
            {
                _deletingGoalId = null;
                StateHasChanged();
            }),
            null,
            3000,
            Timeout.Infinite);
    }

    private void ClearGoalDeleteConfirmation()
    {
        _deletingGoalId = null;
        _deleteConfirmTimer?.Dispose();
        _deleteConfirmTimer = null;
    }


    // ---- Child Goal (Objective) Methods ----

    private void ToggleChildGoalExpand(Guid childGoalId)
    {
        if (!_expandedChildGoals.Add(childGoalId))
        {
            _expandedChildGoals.Remove(childGoalId);
        }
    }

}
