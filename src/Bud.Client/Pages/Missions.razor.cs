using Bud.Client.Services;
using Bud.Client.Shared.Missions;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

#pragma warning disable IDE0011, IDE0044, CA1822, CA1859, CA1860, CA1868

namespace Bud.Client.Pages;

public partial class Missions
{
    // Data
    private PagedResult<Mission>? _missions;
    private Dictionary<Guid, MissionProgressDto> _missionProgress = new();
    private List<Organization> _organizations = new();
    private List<Workspace> _workspaces = new();
    private List<Team> _teams = new();
    private List<Collaborator> _collaborators = new();
    private List<ObjectiveDimension> _objectiveDimensions = new();

    // Filter state
    private string? _filterScopeTypeValue;
    private string? _filterScopeId;
    private string? _search;
    private string _viewMode = "list";
    private bool _showMyMissions = true;
    private bool _filterActiveOnly = true;
    private DateTime? _filterStartDate;
    private DateTime? _filterEndDate;

    // Card expansion state
    private HashSet<Guid> _expandedMissions = new();
    private Dictionary<Guid, List<MissionMetric>> _missionMetricsCache = new();
    private Dictionary<Guid, MetricProgressDto> _metricProgressCache = new();
    private Dictionary<Guid, List<MissionObjective>> _missionObjectivesCache = new();
    private Dictionary<Guid, ObjectiveProgressDto> _objectiveProgressCache = new();
    private HashSet<Guid> _expandedObjectives = new();

    // Wizard state
    private bool _isWizardOpen;
    private bool _isEditMode;
    private Guid? _editingMissionId;
    private MissionWizardModel? _wizardInitialModel;

    // Template picker state
    private bool _isTemplatePickerOpen;
    private List<MissionTemplate> _availableTemplates = new();

    // Checkin modal state
    private bool _isCheckinModalOpen;
    private Mission? _checkinMission;
    private MissionMetric? _selectedCheckinMetric;

    // Checkin history modal state
    private bool _isCheckinHistoryModalOpen;
    private MissionMetric? _historyMetric;
    private Mission? _historyMission;
    private List<MetricCheckin> _metricCheckins = new();
    private bool _isLoadingCheckins;

    // Delete confirmation state
    private Guid? _deletingMissionId;
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
        await LoadMissions();
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
                await LoadMissions();
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
        _organizations = orgsResult?.Items.ToList() ?? new List<Organization>();

        var workspacesResult = await Api.GetWorkspacesAsync(orgId, null, 1, 100);
        _workspaces = workspacesResult?.Items.ToList() ?? new List<Workspace>();

        var teamsResult = await Api.GetTeamsAsync(null, null, null, 1, 100);
        _teams = teamsResult?.Items.ToList() ?? new List<Team>();

        var collaboratorsResult = await Api.GetCollaboratorsAsync(null, null, 1, 100);
        _collaborators = collaboratorsResult?.Items.ToList() ?? new List<Collaborator>();

        var objectiveDimensionsResult = await Api.GetObjectiveDimensionsAsync(null, 1, 100);
        _objectiveDimensions = objectiveDimensionsResult?.Items.ToList() ?? new List<ObjectiveDimension>();
    }

    private async Task SetMissionViewMode(bool myMissions)
    {
        if (_showMyMissions == myMissions)
        {
            return;
        }

        _showMyMissions = myMissions;

        if (_showMyMissions)
        {
            _filterScopeTypeValue = null;
            _filterScopeId = null;
        }

        await LoadMissions();
    }

    private async Task LoadMissions()
    {
        PagedResult<Mission> result;

        if (_showMyMissions)
        {
            var collaboratorId = AuthState.Session?.CollaboratorId;
            if (collaboratorId.HasValue)
            {
                result = await Api.GetMyMissionsAsync(collaboratorId.Value, _search, 1, 100) ?? new PagedResult<Mission>();
            }
            else
            {
                // Global admin without CollaboratorId — fallback to all _missions
                result = await Api.GetMissionsAsync(null, null, _search, 1, 100) ?? new PagedResult<Mission>();
            }
        }
        else
        {
            var scopeType = ParseScopeType(_filterScopeTypeValue);
            var scopeId = Guid.TryParse(_filterScopeId, out var parsedScopeId)
                ? parsedScopeId
                : (Guid?)null;

            result = await Api.GetMissionsAsync(scopeType, scopeId, _search, 1, 100) ?? new PagedResult<Mission>();
        }

        // Apply client-side filters
        var filteredItems = result.Items.AsEnumerable();

        // Filter by active status
        if (_filterActiveOnly)
        {
            filteredItems = filteredItems.Where(m => m.Status == MissionStatus.Active);
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
        _missions = new PagedResult<Mission>
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
        if (_missions is null || _missions.Items.Count == 0)
        {
            _missionProgress = new();
            return;
        }

        try
        {
            var ids = _missions.Items.Select(m => m.Id).ToList();
            var progressList = await Api.GetMissionProgressAsync(ids);
            _missionProgress = progressList?.ToDictionary(p => p.MissionId) ?? new();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao carregar progresso das missões: {ex.Message}");
            _missionProgress = new();
        }
    }

    private async Task LoadMetricProgress(Guid missionId)
    {
        if (!_missionMetricsCache.TryGetValue(missionId, out var metrics) || metrics.Count == 0)
            return;

        try
        {
            var metricIds = metrics.Select(m => m.Id).ToList();
            var progressList = await Api.GetMetricProgressAsync(metricIds);
            if (progressList != null)
            {
                foreach (var progress in progressList)
                {
                    _metricProgressCache[progress.MetricId] = progress;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao carregar progresso das métricas: {ex.Message}");
        }
    }



    private void HandleToggleViewMode()
    {
        _viewMode = _viewMode == "list" ? "grid" : "list";
    }

    private async Task HandleToggleActiveFilter()
    {
        _filterActiveOnly = !_filterActiveOnly;
        await LoadMissions();
    }

    private async Task HandleDateFilterApplied((DateTime?, DateTime?) dates)
    {
        _filterStartDate = dates.Item1;
        _filterEndDate = dates.Item2;
        await LoadMissions();
    }

    private async Task HandleFilterScopeTypeChanged(string? value)
    {
        _filterScopeTypeValue = value;
        _filterScopeId = null;
        await LoadMissions();
    }

    private async Task HandleFilterScopeIdChanged(string? value)
    {
        _filterScopeId = value;
        await LoadMissions();
    }

    private async Task HandleSearchSubmit(string? value)
    {
        _search = value;
        await LoadMissions();
    }

    private async Task HandleClearFilters()
    {
        _filterScopeTypeValue = null;
        _filterScopeId = null;
        _search = null;
        _filterActiveOnly = true;
        _filterStartDate = null;
        _filterEndDate = null;
        await LoadMissions();
    }

    private async Task OpenCreateModal()
    {
        // Load available templates and open the picker
        try
        {
            var result = await Api.GetMissionTemplatesAsync(null, 1, 50);
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
        _wizardInitialModel = new MissionWizardModel
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(7)
        };
        _isEditMode = false;
        _editingMissionId = null;
        _isWizardOpen = true;
    }

    private void OpenCreateModalFromTemplate(MissionTemplate template)
    {
        _isTemplatePickerOpen = false;
        var objectiveIdToTempId = template.Objectives
            .ToDictionary(o => o.Id, _ => Guid.NewGuid().ToString());

        _wizardInitialModel = new MissionWizardModel
        {
            Name = template.MissionNamePattern,
            Description = template.MissionDescriptionPattern,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(90),
            Objectives = template.Objectives
                .OrderBy(o => o.OrderIndex)
                .Select(o => new TempObjective(
                    objectiveIdToTempId[o.Id],
                    o.Name,
                    o.Description,
                    null,
                    o.ObjectiveDimensionId))
                .ToList(),
            Metrics = template.Metrics
                .OrderBy(m => m.OrderIndex)
                .Select(m => new TempMetric(
                    null, m.Name, m.Type.ToString(), GetTemplateMetricDetails(m),
                    m.QuantitativeType?.ToString(), m.MinValue, m.MaxValue, m.TargetText, m.Unit?.ToString(),
                    m.MissionTemplateObjectiveId.HasValue && objectiveIdToTempId.TryGetValue(m.MissionTemplateObjectiveId.Value, out var objectiveTempId)
                        ? objectiveTempId
                        : null))
                .ToList()
        };
        _isEditMode = false;
        _editingMissionId = null;
        _isWizardOpen = true;
    }

    private static string GetTemplateMetricDetails(MissionTemplateMetric metric)
    {
        if (metric.Type == MetricType.Qualitative)
            return $"Qualitativa — {metric.TargetText}";

        var parts = new List<string> { "Quantitativa" };
        if (metric.QuantitativeType.HasValue)
            parts.Add(metric.QuantitativeType.Value.ToString());
        if (metric.Unit.HasValue)
            parts.Add(metric.Unit.Value.ToString());
        if (metric.MinValue.HasValue)
            parts.Add($"Min: {metric.MinValue}");
        if (metric.MaxValue.HasValue)
            parts.Add($"Max: {metric.MaxValue}");
        return string.Join(" — ", parts);
    }

    private void CloseWizard()
    {
        _isWizardOpen = false;
        _isEditMode = false;
        _editingMissionId = null;
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

    private async Task HandleWizardSave(MissionWizardResult result)
    {
        if (_isEditMode && _editingMissionId.HasValue)
        {
            await UpdateMissionFromResult(_editingMissionId.Value, result);
        }
        else
        {
            await CreateMissionFromResult(result, MissionStatus.Active,
                "Missão criada com sucesso!",
                m => $"A missão '{m?.Name}' foi criada e está ativa.",
                "Erro ao criar missão",
                "Não foi possível criar a missão. Verifique os dados e tente novamente.");
        }
    }

    private async Task HandleWizardSaveDraft(MissionWizardResult result)
    {
        await CreateMissionFromResult(result, MissionStatus.Planned,
            "Rascunho salvo com sucesso!",
            _ => "A missão foi salva como rascunho.",
            "Erro ao salvar rascunho",
            "Não foi possível salvar o rascunho da missão. Verifique os dados e tente novamente.");
    }

    private async Task CreateMissionFromResult(
        MissionWizardResult result,
        MissionStatus status,
        string successTitle,
        Func<Mission?, string> successMessageFactory,
        string errorTitle,
        string errorMessage)
    {
        if (!Enum.TryParse<MissionScopeType>(result.ScopeTypeValue, out var scopeType)) return;
        if (!Guid.TryParse(result.ScopeId, out var scopeId)) return;

        await UiOps.RunAsync(
            async () =>
            {
                var request = new CreateMissionRequest
                {
                    Name = result.Name,
                    Description = result.Description,
                    StartDate = result.StartDate,
                    EndDate = result.EndDate,
                    ScopeType = scopeType,
                    ScopeId = scopeId,
                    Status = status
                };

                var createdMission = await Api.CreateMissionAsync(request);
                await CreateObjectivesAndMetricsForMission(createdMission, result.Objectives, result.Metrics);

                await LoadMissions();
                ToastService.ShowSuccess(successTitle, successMessageFactory(createdMission));
                CloseWizard();
            },
            errorTitle,
            errorMessage);
    }

    private async Task CreateObjectivesAndMetricsForMission(Mission? createdMission, List<TempObjective> objectives, List<TempMetric> metrics)
    {
        if (createdMission is null) return;

        var (objectiveIdMap, objectiveFailedCount) = await CreateObjectivesAsync(createdMission.Id, objectives);
        var metricFailedCount = await CreateMetricsAsync(createdMission.Id, metrics, objectiveIdMap);
        ShowPartialFailureIfAny(objectiveFailedCount + metricFailedCount);
    }

    private async Task<(Dictionary<string, Guid> objectiveIdMap, int failedCount)> CreateObjectivesAsync(Guid missionId, List<TempObjective> objectives)
    {
        var failedCount = 0;
        var objectiveIdMap = new Dictionary<string, Guid>();

        foreach (var tempObj in objectives)
        {
            try
            {
                var created = await Api.CreateMissionObjectiveAsync(new CreateMissionObjectiveRequest
                {
                    MissionId = missionId,
                    Name = tempObj.Name,
                    Description = tempObj.Description,
                    ObjectiveDimensionId = tempObj.ObjectiveDimensionId
                });
                if (created is not null)
                {
                    objectiveIdMap[tempObj.TempId] = created.Id;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erro ao criar objetivo '{tempObj.Name}' para missão {missionId}: {ex.Message}");
                failedCount++;
            }
        }

        return (objectiveIdMap, failedCount);
    }

    private async Task<int> CreateMetricsAsync(Guid missionId, List<TempMetric> metrics, Dictionary<string, Guid> objectiveIdMap)
    {
        var failedCount = 0;
        foreach (var tempMetric in metrics)
        {
            var request = BuildCreateMetricRequest(missionId, tempMetric);
            if (request is null)
            {
                failedCount++;
                continue;
            }

            if (tempMetric.ObjectiveTempId is not null && objectiveIdMap.TryGetValue(tempMetric.ObjectiveTempId, out var objectiveId))
            {
                request.MissionObjectiveId = objectiveId;
            }

            try
            {
                await Api.CreateMissionMetricAsync(request);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erro ao criar métrica '{tempMetric.Name}' para missão {missionId}: {ex.Message}");
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
        if (!Enum.TryParse<MissionScopeType>(scopeTypeValue, out var scopeType))
        {
            return Enumerable.Empty<ScopeOption>();
        }

        return scopeType switch
        {
            MissionScopeType.Organization => _organizations.Select(o => new ScopeOption(o.Id.ToString(), o.Name)),
            MissionScopeType.Workspace => _workspaces.Select(w => new ScopeOption(w.Id.ToString(), w.Name)),
            MissionScopeType.Team => _teams.Select(t => new ScopeOption(t.Id.ToString(), t.Name)),
            MissionScopeType.Collaborator => _collaborators.Select(c => new ScopeOption(c.Id.ToString(), c.FullName)),
            _ => Enumerable.Empty<ScopeOption>()
        };
    }

    private MissionScopeType? ParseScopeType(string? scopeTypeValue)
    {
        return Enum.TryParse<MissionScopeType>(scopeTypeValue, out var scopeType) ? scopeType : null;
    }


    // ---- Card Expansion Methods ----

    private async Task ToggleExpand(Guid missionId)
    {
        if (_expandedMissions.Contains(missionId))
        {
            _expandedMissions.Remove(missionId);
            return;
        }

        await ExpandMissionAsync(missionId);
    }

    private async Task ExpandMissionAsync(Guid missionId)
    {
        _expandedMissions.Add(missionId);
        if (_missionMetricsCache.ContainsKey(missionId))
        {
            return;
        }

        var (metrics, objectives) = await LoadMissionDetailsIntoCacheAsync(missionId);
        await LoadCardProgressAsync(metrics, objectives);
        StateHasChanged();
    }

    private async Task<(List<MissionMetric> metrics, List<MissionObjective> objectives)> LoadMissionDetailsIntoCacheAsync(Guid missionId)
    {
        var metricsTask = Api.GetMissionMetricsByMissionIdAsync(missionId);
        var objectivesTask = Api.GetMissionObjectivesAsync(missionId);
        await Task.WhenAll(metricsTask, objectivesTask);

        var metrics = metricsTask.Result?.Items.ToList() ?? [];
        var objectives = objectivesTask.Result?.Items.ToList() ?? [];

        _missionMetricsCache[missionId] = metrics;
        _missionObjectivesCache[missionId] = objectives;

        return (metrics, objectives);
    }

    private async Task LoadCardProgressAsync(List<MissionMetric> metrics, List<MissionObjective> objectives)
    {
        var metricProgressTask = metrics.Count > 0
            ? LoadMetricProgressForIds(metrics.Select(m => m.Id).ToList())
            : Task.CompletedTask;

        var objectiveProgressTask = objectives.Count > 0
            ? LoadObjectiveProgress(objectives.Select(o => o.Id).ToList())
            : Task.CompletedTask;

        await Task.WhenAll(metricProgressTask, objectiveProgressTask);
    }

    private async Task LoadObjectiveProgress(List<Guid> objectiveIds)
    {
        var progressList = await Api.GetObjectiveProgressAsync(objectiveIds);
        if (progressList != null)
        {
            foreach (var progress in progressList)
            {
                _objectiveProgressCache[progress.ObjectiveId] = progress;
            }
        }
    }

    private async Task LoadMetricProgressForIds(List<Guid> metricIds)
    {
        var progressList = await Api.GetMetricProgressAsync(metricIds);
        if (progressList != null)
        {
            foreach (var progress in progressList)
            {
                _metricProgressCache[progress.MetricId] = progress;
            }
        }
    }

    // ---- Checkin History Modal Methods ----

    private async Task OpenCheckinHistoryModal(Mission mission, MissionMetric metric)
    {
        _historyMission = mission;
        _historyMetric = metric;
        _isCheckinHistoryModalOpen = true;
        _isLoadingCheckins = true;
        _metricCheckins = new List<MetricCheckin>();
        StateHasChanged();

        try
        {
            var result = await Api.GetMetricCheckinsAsync(metric.Id, null, 1, 50);
            _metricCheckins = result?.Items.OrderByDescending(c => c.CheckinDate).ToList() ?? new List<MetricCheckin>();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao carregar histórico de check-ins da métrica {metric.Id}: {ex.Message}");
            _metricCheckins = new List<MetricCheckin>();
            ToastService.ShowError("Erro ao carregar histórico", "Não foi possível carregar o histórico de check-ins desta métrica.");
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
        _historyMetric = null;
        _historyMission = null;
        _metricCheckins = new List<MetricCheckin>();
    }

    private void OpenCheckinFromHistory()
    {
        // Capture values before closing the history modal
        var mission = _historyMission;
        var metric = _historyMetric;

        // Close history modal first
        _isCheckinHistoryModalOpen = false;
        _historyMetric = null;
        _historyMission = null;
        _metricCheckins = new List<MetricCheckin>();

        // Now open checkin modal with captured values
        if (mission != null && metric != null)
        {
            OpenCheckinModalForMetric(mission, metric);
        }
    }

    // ---- Summary Card Methods ----

    private int GetOverallProgressDisplay()
    {
        if (_missions is null || !_missionProgress.Any()) return 0;
        var activeProgress = _missionProgress.Values.Where(p => p.TotalMetrics > 0).ToList();
        if (!activeProgress.Any()) return 0;
        return (int)activeProgress.Average(p => p.OverallProgress);
    }

    private int GetExpectedProgressDisplay()
    {
        if (_missions is null || !_missionProgress.Any()) return 0;
        var activeProgress = _missionProgress.Values.Where(p => p.ExpectedProgress > 0).ToList();
        if (!activeProgress.Any()) return 0;
        return (int)activeProgress.Average(p => p.ExpectedProgress);
    }

    private int GetActiveMissionsCount()
    {
        return _missions?.Items.Count(m => m.Status == MissionStatus.Active) ?? 0;
    }

    private int GetOutdatedMetricsCount()
    {
        return _missionProgress.Values.Sum(p => p.OutdatedMetrics);
    }

    // ---- Checkin Modal Methods ----

    private void OpenCheckinModalForMetric(Mission mission, MissionMetric metric)
    {
        _checkinMission = mission;
        _selectedCheckinMetric = metric;
        _isCheckinModalOpen = true;
    }

    private void CloseCheckinModal()
    {
        _isCheckinModalOpen = false;
        _checkinMission = null;
        _selectedCheckinMetric = null;
    }

    private async Task HandleCheckinSubmit(CreateMetricCheckinRequest request)
    {
        if (request.ConfidenceLevel < 1 || request.ConfidenceLevel > 5)
        {
            ToastService.ShowError("Erro ao criar check-in", "Selecione o nível de confiança (1-5).");
            return;
        }

        await UiOps.RunAsync(
            async () =>
            {
                await Api.CreateMetricCheckinAsync(request);
                ToastService.ShowSuccess("Check-in criado", "O check-in foi registrado com sucesso.");
                var missionId = _checkinMission?.Id;
                CloseCheckinModal();
                await LoadProgress();
                if (missionId.HasValue)
                {
                    await LoadMetricProgress(missionId.Value);
                }
            },
            "Erro ao criar check-in",
            "Não foi possível criar o check-in. Verifique os dados e tente novamente.");
    }

    // ---- Mission Edit/Delete Methods ----

    private (MissionScopeType scopeType, Guid scopeId) GetMissionScope(Mission mission)
    {
        if (mission.CollaboratorId.HasValue)
            return (MissionScopeType.Collaborator, mission.CollaboratorId.Value);
        if (mission.TeamId.HasValue)
            return (MissionScopeType.Team, mission.TeamId.Value);
        if (mission.WorkspaceId.HasValue)
            return (MissionScopeType.Workspace, mission.WorkspaceId.Value);
        return (MissionScopeType.Organization, mission.OrganizationId);
    }

    private async Task OpenEditWizard(Mission mission)
    {
        _isEditMode = true;
        _editingMissionId = mission.Id;

        var (scopeType, scopeId) = GetMissionScope(mission);

        // Load metrics and objectives in parallel
        var metricsTask = Api.GetMissionMetricsByMissionIdAsync(mission.Id);
        var objectivesTask = Api.GetMissionObjectivesAsync(mission.Id);
        await Task.WhenAll(metricsTask, objectivesTask);

        var apiMetrics = metricsTask.Result?.Items.ToList() ?? new List<MissionMetric>();
        var apiObjectives = objectivesTask.Result?.Items.ToList() ?? new List<MissionObjective>();

        // Convert objectives to TempObjective with OriginalId
        var tempObjs = apiObjectives.Select(o => new TempObjective(
            TempId: Guid.NewGuid().ToString(),
            Name: o.Name,
            Description: o.Description,
            OriginalId: o.Id,
            ObjectiveDimensionId: o.ObjectiveDimensionId
        )).ToList();

        // Build a map from original objective ID to TempId
        var objectiveIdToTempId = tempObjs
            .Where(o => o.OriginalId.HasValue)
            .ToDictionary(o => o.OriginalId!.Value, o => o.TempId);

        // Convert metrics to TempMetric with OriginalId and resolved ObjectiveTempId
        var tempMets = apiMetrics.Select(m =>
        {
            string? objTempId = null;
            if (m.MissionObjectiveId.HasValue && objectiveIdToTempId.TryGetValue(m.MissionObjectiveId.Value, out var tid))
            {
                objTempId = tid;
            }
            return new TempMetric(
                OriginalId: m.Id,
                Name: m.Name,
                Type: m.Type.ToString(),
                Details: GetMetricDetailsFromModel(m),
                QuantitativeType: m.QuantitativeType?.ToString(),
                MinValue: m.MinValue,
                MaxValue: m.MaxValue,
                TargetText: m.TargetText,
                Unit: m.Unit?.ToString(),
                ObjectiveTempId: objTempId);
        }).ToList();

        _wizardInitialModel = new MissionWizardModel
        {
            Name = mission.Name,
            Description = mission.Description,
            StartDate = mission.StartDate,
            EndDate = mission.EndDate,
            ScopeTypeValue = scopeType.ToString(),
            ScopeId = scopeId.ToString(),
            StatusValue = mission.Status.ToString(),
            Metrics = tempMets,
            Objectives = tempObjs
        };

        _isWizardOpen = true;
    }

    private async Task UpdateMissionFromResult(Guid missionId, MissionWizardResult result)
    {
        if (!Enum.TryParse<MissionScopeType>(result.ScopeTypeValue, out var scopeType)) return;
        if (!Guid.TryParse(result.ScopeId, out var scopeId)) return;

        await UiOps.RunAsync(
            async () =>
            {
                await Api.UpdateMissionAsync(missionId, BuildUpdateMissionRequest(result, scopeType, scopeId));

                var (objectiveIdMap, objectiveFailureCount) = await ProcessObjectiveChangesAsync(missionId, result);
                var metricFailureCount = await ProcessMetricChangesAsync(missionId, result, objectiveIdMap);

                ShowPartialFailureIfAny(objectiveFailureCount + metricFailureCount);
                await RefreshMissionCachesAsync(missionId);

                ToastService.ShowSuccess("Missão atualizada", "As alterações foram salvas com sucesso.");
                CloseWizard();
            },
            "Erro ao atualizar",
            "Não foi possível atualizar a missão. Verifique os dados e tente novamente.");
    }

    private static UpdateMissionRequest BuildUpdateMissionRequest(MissionWizardResult result, MissionScopeType scopeType, Guid scopeId)
    {
        var request = new UpdateMissionRequest
        {
            Name = result.Name,
            Description = result.Description,
            StartDate = result.StartDate,
            EndDate = result.EndDate,
            ScopeType = scopeType,
            ScopeId = scopeId
        };

        if (Enum.TryParse<MissionStatus>(result.StatusValue, out var status))
        {
            request.Status = status;
        }

        return request;
    }

    private async Task<(Dictionary<string, Guid> objectiveIdMap, int failureCount)> ProcessObjectiveChangesAsync(Guid missionId, MissionWizardResult result)
    {
        var failureCount = 0;
        var objectiveIdMap = new Dictionary<string, Guid>();

        foreach (var objectiveId in result.DeletedObjectiveIds)
        {
            try
            {
                await Api.DeleteMissionObjectiveAsync(objectiveId);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erro ao excluir objetivo {objectiveId}: {ex.Message}");
                failureCount++;
            }
        }

        foreach (var objective in result.Objectives.Where(o => o.OriginalId.HasValue))
        {
            objectiveIdMap[objective.TempId] = objective.OriginalId!.Value;
            try
            {
                await Api.UpdateMissionObjectiveAsync(objective.OriginalId.Value, new UpdateMissionObjectiveRequest
                {
                    Name = objective.Name,
                    Description = objective.Description,
                    ObjectiveDimensionId = objective.ObjectiveDimensionId
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erro ao atualizar objetivo {objective.OriginalId}: {ex.Message}");
                failureCount++;
            }
        }

        foreach (var objective in result.Objectives.Where(o => !o.OriginalId.HasValue))
        {
            try
            {
                var created = await Api.CreateMissionObjectiveAsync(new CreateMissionObjectiveRequest
                {
                    MissionId = missionId,
                    Name = objective.Name,
                    Description = objective.Description,
                    ObjectiveDimensionId = objective.ObjectiveDimensionId
                });

                if (created is not null)
                {
                    objectiveIdMap[objective.TempId] = created.Id;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erro ao criar objetivo '{objective.Name}': {ex.Message}");
                failureCount++;
            }
        }

        return (objectiveIdMap, failureCount);
    }

    private async Task<int> ProcessMetricChangesAsync(Guid missionId, MissionWizardResult result, Dictionary<string, Guid> objectiveIdMap)
    {
        var failureCount = 0;

        foreach (var metricId in result.DeletedMetricIds)
        {
            try
            {
                await Api.DeleteMissionMetricAsync(metricId);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erro ao excluir métrica {metricId}: {ex.Message}");
                failureCount++;
            }
        }

        foreach (var metric in result.Metrics.Where(m => m.OriginalId.HasValue))
        {
            try
            {
                var request = BuildUpdateMetricRequest(metric);
                if (request is not null)
                {
                    await Api.UpdateMissionMetricAsync(metric.OriginalId!.Value, request);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erro ao atualizar métrica {metric.OriginalId}: {ex.Message}");
                failureCount++;
            }
        }

        foreach (var metric in result.Metrics.Where(m => !m.OriginalId.HasValue))
        {
            try
            {
                var request = BuildCreateMetricRequest(missionId, metric);
                if (request is null)
                {
                    failureCount++;
                    continue;
                }

                if (metric.ObjectiveTempId is not null && objectiveIdMap.TryGetValue(metric.ObjectiveTempId, out var objectiveId))
                {
                    request.MissionObjectiveId = objectiveId;
                }

                await Api.CreateMissionMetricAsync(request);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erro ao criar métrica '{metric.Name}': {ex.Message}");
                failureCount++;
            }
        }

        return failureCount;
    }

    private async Task RefreshMissionCachesAsync(Guid missionId)
    {
        _missionMetricsCache.Remove(missionId);
        _missionObjectivesCache.Remove(missionId);
        await LoadMissions();

        if (!_expandedMissions.Contains(missionId))
        {
            return;
        }

        var (metrics, objectives) = await LoadMissionDetailsIntoCacheAsync(missionId);
        if (metrics.Count > 0)
        {
            await LoadMetricProgress(missionId);
        }

        if (objectives.Count > 0)
        {
            await LoadObjectiveProgress(objectives.Select(o => o.Id).ToList());
        }
    }

    private async Task HandleDeleteClick(Guid missionId)
    {
        if (_deletingMissionId == missionId)
        {
            await DeleteMission(missionId);
        }
        else
        {
            ArmMissionDeleteConfirmation(missionId);
        }
    }

    private async Task DeleteMission(Guid missionId)
    {
        try
        {
            await UiOps.RunAsync(
                async () =>
                {
                    await Api.DeleteMissionAsync(missionId);
                    ToastService.ShowSuccess("Missão excluída", "A missão foi removida com sucesso.");
                    await LoadMissions();
                },
            "Erro ao excluir",
            "Não foi possível excluir a missão. Tente novamente.");
        }
        finally
        {
            ClearMissionDeleteConfirmation();
        }
    }

    private static string GetMetricDetailsFromModel(MissionMetric m)
    {
        if (m.Type == MetricType.Qualitative)
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

    private CreateMissionMetricRequest? BuildCreateMetricRequest(Guid missionId, TempMetric metric)
    {
        if (!EnumParsingHelper.TryParseEnum<MetricType>(metric.Type, out var metricType))
            return null;

        var request = new CreateMissionMetricRequest
        {
            MissionId = missionId,
            Name = metric.Name,
            Type = metricType,
            MinValue = metric.MinValue,
            MaxValue = metric.MaxValue,
            TargetText = metric.TargetText
        };

        if (metricType == MetricType.Quantitative)
        {
            if (!string.IsNullOrEmpty(metric.QuantitativeType) &&
                EnumParsingHelper.TryParseEnum<QuantitativeMetricType>(metric.QuantitativeType, out var qType))
                request.QuantitativeType = qType;
            if (!string.IsNullOrEmpty(metric.Unit) &&
                EnumParsingHelper.TryParseEnum<MetricUnit>(metric.Unit, out var unit))
                request.Unit = unit;
        }

        return request;
    }

    private UpdateMissionMetricRequest? BuildUpdateMetricRequest(TempMetric metric)
    {
        if (!EnumParsingHelper.TryParseEnum<MetricType>(metric.Type, out var metricType))
            return null;

        var request = new UpdateMissionMetricRequest
        {
            Name = metric.Name,
            Type = metricType,
            MinValue = metric.MinValue,
            MaxValue = metric.MaxValue,
            TargetText = metric.TargetText
        };

        if (metricType == MetricType.Quantitative)
        {
            if (!string.IsNullOrEmpty(metric.QuantitativeType) &&
                EnumParsingHelper.TryParseEnum<QuantitativeMetricType>(metric.QuantitativeType, out var qType))
                request.QuantitativeType = qType;
            if (!string.IsNullOrEmpty(metric.Unit) &&
                EnumParsingHelper.TryParseEnum<MetricUnit>(metric.Unit, out var unit))
                request.Unit = unit;
        }

        return request;
    }

    private void ArmMissionDeleteConfirmation(Guid missionId)
    {
        _deletingMissionId = missionId;
        _deleteConfirmTimer?.Dispose();
        _deleteConfirmTimer = new System.Threading.Timer(
            _ => InvokeAsync(() =>
            {
                _deletingMissionId = null;
                StateHasChanged();
            }),
            null,
            3000,
            Timeout.Infinite);
    }

    private void ClearMissionDeleteConfirmation()
    {
        _deletingMissionId = null;
        _deleteConfirmTimer?.Dispose();
        _deleteConfirmTimer = null;
    }


    // ---- Objective Methods ----

    private void ToggleObjectiveExpand(Guid objectiveId)
    {
        if (!_expandedObjectives.Add(objectiveId))
        {
            _expandedObjectives.Remove(objectiveId);
        }
    }

}
