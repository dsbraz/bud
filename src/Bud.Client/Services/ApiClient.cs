using System.Net.Http.Json;
using System.Text.Json;
using Bud.Shared.Contracts;

namespace Bud.Client.Services;

public sealed class ApiClient
{
    private const int MaxPageSize = 100;
    private readonly HttpClient _http;
    private readonly ToastService _toastService;
    private DateTime _lastLoadWarningUtc = DateTime.MinValue;

    public ApiClient(HttpClient http, ToastService toastService)
    {
        _http = http;
        _toastService = toastService;
    }

    public async Task<List<OrganizationSummaryDto>?> GetMyOrganizationsAsync()
    {
        return await GetSafeAsync<List<OrganizationSummaryDto>>("api/auth/my-organizations");
    }

    public async Task<PagedResult<Organization>?> GetOrganizationsAsync(string? search, int page = 1, int pageSize = 10)
    {
        (page, pageSize) = NormalizePagination(page, pageSize);

        var queryParams = new List<string>();

        if (!string.IsNullOrEmpty(search))
        {
            queryParams.Add($"search={Uri.EscapeDataString(search)}");
        }

        queryParams.Add($"page={page}");
        queryParams.Add($"pageSize={pageSize}");

        var url = $"api/organizations?{string.Join("&", queryParams)}";
        return await GetSafeAsync<PagedResult<Organization>>(url);
    }

    public async Task<Organization?> CreateOrganizationAsync(CreateOrganizationRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/organizations", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }

        return await response.Content.ReadFromJsonAsync<Organization>();
    }

    public async Task<Organization?> UpdateOrganizationAsync(Guid id, UpdateOrganizationRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/organizations/{id}", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }

        return await response.Content.ReadFromJsonAsync<Organization>();
    }

    public async Task DeleteOrganizationAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/organizations/{id}");

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }
    }

    public async Task<List<LeaderCollaboratorResponse>?> GetLeadersAsync(Guid? organizationId = null)
    {
        var url = "api/collaborators/leaders";
        if (organizationId.HasValue)
        {
            url += $"?organizationId={organizationId.Value}";
        }
        return await GetSafeAsync<List<LeaderCollaboratorResponse>>(url);
    }

    public async Task<PagedResult<Workspace>?> GetWorkspacesAsync(Guid? organizationId, string? search, int page = 1, int pageSize = 10)
    {
        (page, pageSize) = NormalizePagination(page, pageSize);

        var queryParams = new List<string>();

        if (organizationId.HasValue)
        {
            queryParams.Add($"organizationId={organizationId.Value}");
        }
        if (!string.IsNullOrEmpty(search))
        {
            queryParams.Add($"search={Uri.EscapeDataString(search)}");
        }

        queryParams.Add($"page={page}");
        queryParams.Add($"pageSize={pageSize}");

        var url = $"api/workspaces?{string.Join("&", queryParams)}";
        return await GetSafeAsync<PagedResult<Workspace>>(url);
    }

    public async Task<PagedResult<Collaborator>?> GetOrganizationCollaboratorsAsync(Guid organizationId, int page = 1, int pageSize = 10)
    {
        (page, pageSize) = NormalizePagination(page, pageSize);
        var url = $"api/organizations/{organizationId}/collaborators?page={page}&pageSize={pageSize}";
        return await GetSafeAsync<PagedResult<Collaborator>>(url);
    }

    public async Task<Workspace?> CreateWorkspaceAsync(CreateWorkspaceRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/workspaces", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }

        return await response.Content.ReadFromJsonAsync<Workspace>();
    }

    public async Task<Workspace?> UpdateWorkspaceAsync(Guid id, UpdateWorkspaceRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/workspaces/{id}", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }

        return await response.Content.ReadFromJsonAsync<Workspace>();
    }

    public async Task DeleteWorkspaceAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/workspaces/{id}");

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }
    }

    public async Task<PagedResult<Team>?> GetTeamsAsync(Guid? workspaceId, Guid? parentTeamId, string? search, int page = 1, int pageSize = 10)
    {
        (page, pageSize) = NormalizePagination(page, pageSize);

        var queryParams = new List<string>();

        if (workspaceId.HasValue)
        {
            queryParams.Add($"workspaceId={workspaceId.Value}");
        }
        if (parentTeamId.HasValue)
        {
            queryParams.Add($"parentTeamId={parentTeamId.Value}");
        }
        if (!string.IsNullOrEmpty(search))
        {
            queryParams.Add($"search={Uri.EscapeDataString(search)}");
        }

        queryParams.Add($"page={page}");
        queryParams.Add($"pageSize={pageSize}");

        var url = $"api/teams?{string.Join("&", queryParams)}";
        return await GetSafeAsync<PagedResult<Team>>(url);
    }

    public async Task<Team?> CreateTeamAsync(CreateTeamRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/teams", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }

        return await response.Content.ReadFromJsonAsync<Team>();
    }

    public async Task<Team?> UpdateTeamAsync(Guid id, UpdateTeamRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/teams/{id}", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }

        return await response.Content.ReadFromJsonAsync<Team>();
    }

    public async Task DeleteTeamAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/teams/{id}");

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }
    }

    public async Task<PagedResult<Collaborator>?> GetCollaboratorsAsync(Guid? teamId, string? search, int page = 1, int pageSize = 10)
    {
        (page, pageSize) = NormalizePagination(page, pageSize);

        var queryParams = new List<string>();

        if (teamId.HasValue)
        {
            queryParams.Add($"teamId={teamId.Value}");
        }
        if (!string.IsNullOrEmpty(search))
        {
            queryParams.Add($"search={Uri.EscapeDataString(search)}");
        }

        queryParams.Add($"page={page}");
        queryParams.Add($"pageSize={pageSize}");

        var url = $"api/collaborators?{string.Join("&", queryParams)}";
        return await GetSafeAsync<PagedResult<Collaborator>>(url);
    }

    public async Task<Collaborator?> CreateCollaboratorAsync(CreateCollaboratorRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/collaborators", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }

        return await response.Content.ReadFromJsonAsync<Collaborator>();
    }

    public async Task<Collaborator?> UpdateCollaboratorAsync(Guid id, UpdateCollaboratorRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/collaborators/{id}", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }

        return await response.Content.ReadFromJsonAsync<Collaborator>();
    }

    public async Task DeleteCollaboratorAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/collaborators/{id}");

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }
    }

    public async Task<PagedResult<Mission>?> GetMissionsAsync(
        MissionScopeType? scopeType,
        Guid? scopeId,
        string? search,
        int page = 1,
        int pageSize = 10)
    {
        (page, pageSize) = NormalizePagination(page, pageSize);

        var queryParams = new List<string>();

        if (scopeType.HasValue)
        {
            queryParams.Add($"scopeType={scopeType.Value}");
        }
        if (scopeId.HasValue)
        {
            queryParams.Add($"scopeId={scopeId.Value}");
        }
        if (!string.IsNullOrEmpty(search))
        {
            queryParams.Add($"search={Uri.EscapeDataString(search)}");
        }

        queryParams.Add($"page={page}");
        queryParams.Add($"pageSize={pageSize}");

        var url = $"api/missions?{string.Join("&", queryParams)}";
        return await GetSafeAsync<PagedResult<Mission>>(url);
    }

    public async Task<PagedResult<Mission>?> GetMyMissionsAsync(
        Guid collaboratorId, string? search, int page = 1, int pageSize = 10)
    {
        (page, pageSize) = NormalizePagination(page, pageSize);

        var queryParams = new List<string>();

        if (!string.IsNullOrEmpty(search))
        {
            queryParams.Add($"search={Uri.EscapeDataString(search)}");
        }

        queryParams.Add($"page={page}");
        queryParams.Add($"pageSize={pageSize}");

        var url = $"api/missions/my-missions/{collaboratorId}?{string.Join("&", queryParams)}";
        return await GetSafeAsync<PagedResult<Mission>>(url);
    }

    public async Task<Mission?> CreateMissionAsync(CreateMissionRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/missions", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }

        return await response.Content.ReadFromJsonAsync<Mission>();
    }

    public async Task<MyDashboardResponse?> GetMyDashboardAsync(Guid? teamId = null)
    {
        var url = "api/dashboard/my-dashboard";
        if (teamId.HasValue)
        {
            url += $"?teamId={teamId.Value}";
        }
        return await GetSafeAsync<MyDashboardResponse>(url);
    }

    public async Task<PagedResult<MissionMetric>?> GetMissionMetricsAsync(Guid? missionId, string? search, int page = 1, int pageSize = 10)
    {
        (page, pageSize) = NormalizePagination(page, pageSize);

        var queryParams = new List<string>();

        if (missionId.HasValue)
        {
            queryParams.Add($"missionId={missionId.Value}");
        }
        if (!string.IsNullOrEmpty(search))
        {
            queryParams.Add($"search={Uri.EscapeDataString(search)}");
        }

        queryParams.Add($"page={page}");
        queryParams.Add($"pageSize={pageSize}");

        var url = $"api/mission-metrics?{string.Join("&", queryParams)}";
        return await GetSafeAsync<PagedResult<MissionMetric>>(url);
    }

    public async Task<MissionMetric?> CreateMissionMetricAsync(CreateMissionMetricRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/mission-metrics", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }

        return await response.Content.ReadFromJsonAsync<MissionMetric>();
    }

    public async Task<Mission?> UpdateMissionAsync(Guid id, UpdateMissionRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/missions/{id}", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }

        return await response.Content.ReadFromJsonAsync<Mission>();
    }

    public async Task DeleteMissionAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/missions/{id}");

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }
    }

    public async Task<MissionMetric?> UpdateMissionMetricAsync(Guid id, UpdateMissionMetricRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/mission-metrics/{id}", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }

        return await response.Content.ReadFromJsonAsync<MissionMetric>();
    }

    public async Task DeleteMissionMetricAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/mission-metrics/{id}");

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }
    }

    public async Task<PagedResult<MissionMetric>?> GetMissionMetricsByMissionIdAsync(Guid missionId, int page = 1, int pageSize = 100)
    {
        (page, pageSize) = NormalizePagination(page, pageSize);
        var url = $"api/missions/{missionId}/metrics?page={page}&pageSize={pageSize}";
        return await GetSafeAsync<PagedResult<MissionMetric>>(url);
    }

    // MissionObjective methods
    public async Task<PagedResult<MissionObjective>?> GetMissionObjectivesAsync(Guid missionId, int page = 1, int pageSize = 100)
    {
        (page, pageSize) = NormalizePagination(page, pageSize);

        var queryParams = new List<string>
        {
            $"missionId={missionId}",
            $"page={page}",
            $"pageSize={pageSize}"
        };

        var url = $"api/mission-objectives?{string.Join("&", queryParams)}";
        return await GetSafeAsync<PagedResult<MissionObjective>>(url);
    }

    public async Task<MissionObjective?> CreateMissionObjectiveAsync(CreateMissionObjectiveRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/mission-objectives", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }

        return await response.Content.ReadFromJsonAsync<MissionObjective>();
    }

    public async Task<MissionObjective?> UpdateMissionObjectiveAsync(Guid id, UpdateMissionObjectiveRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/mission-objectives/{id}", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }

        return await response.Content.ReadFromJsonAsync<MissionObjective>();
    }

    public async Task DeleteMissionObjectiveAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/mission-objectives/{id}");

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }
    }

    public async Task<List<ObjectiveProgressDto>?> GetObjectiveProgressAsync(List<Guid> objectiveIds)
    {
        if (objectiveIds.Count == 0)
        {
            return [];
        }

        var ids = string.Join(",", objectiveIds);
        return await GetSafeAsync<List<ObjectiveProgressDto>>($"api/mission-objectives/progress?ids={ids}");
    }

    // Metric Progress
    public async Task<List<MetricProgressDto>?> GetMetricProgressAsync(List<Guid> metricIds)
    {
        if (metricIds.Count == 0)
        {
            return [];
        }

        var ids = string.Join(",", metricIds);
        return await GetSafeAsync<List<MetricProgressDto>>($"api/mission-metrics/progress?ids={ids}");
    }

    // Mission Progress
    public async Task<List<MissionProgressDto>?> GetMissionProgressAsync(List<Guid> missionIds)
    {
        if (missionIds.Count == 0)
        {
            return [];
        }

        var ids = string.Join(",", missionIds);
        return await GetSafeAsync<List<MissionProgressDto>>($"api/missions/progress?ids={ids}");
    }

    // MetricCheckin methods
    public async Task<PagedResult<MetricCheckin>?> GetMetricCheckinsAsync(Guid? missionMetricId, Guid? missionId, int page = 1, int pageSize = 10)
    {
        (page, pageSize) = NormalizePagination(page, pageSize);

        var queryParams = new List<string>();

        if (missionMetricId.HasValue)
        {
            queryParams.Add($"missionMetricId={missionMetricId.Value}");
        }
        if (missionId.HasValue)
        {
            queryParams.Add($"missionId={missionId.Value}");
        }

        queryParams.Add($"page={page}");
        queryParams.Add($"pageSize={pageSize}");

        var url = $"api/metric-checkins?{string.Join("&", queryParams)}";
        return await GetSafeAsync<PagedResult<MetricCheckin>>(url);
    }

    public async Task<MetricCheckin?> CreateMetricCheckinAsync(CreateMetricCheckinRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/metric-checkins", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }

        return await response.Content.ReadFromJsonAsync<MetricCheckin>();
    }

    public async Task<MetricCheckin?> UpdateMetricCheckinAsync(Guid id, UpdateMetricCheckinRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/metric-checkins/{id}", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }

        return await response.Content.ReadFromJsonAsync<MetricCheckin>();
    }

    public async Task DeleteMetricCheckinAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/metric-checkins/{id}");

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }
    }

    // MissionTemplate methods
    public async Task<PagedResult<MissionTemplate>?> GetMissionTemplatesAsync(string? search, int page = 1, int pageSize = 10)
    {
        (page, pageSize) = NormalizePagination(page, pageSize);

        var queryParams = new List<string>();

        if (!string.IsNullOrEmpty(search))
        {
            queryParams.Add($"search={Uri.EscapeDataString(search)}");
        }

        queryParams.Add($"page={page}");
        queryParams.Add($"pageSize={pageSize}");

        var url = $"api/mission-templates?{string.Join("&", queryParams)}";
        return await GetSafeAsync<PagedResult<MissionTemplate>>(url);
    }

    public async Task<MissionTemplate?> GetMissionTemplateByIdAsync(Guid id)
    {
        return await GetSafeAsync<MissionTemplate>($"api/mission-templates/{id}");
    }

    public async Task<MissionTemplate?> CreateMissionTemplateAsync(CreateMissionTemplateRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/mission-templates", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }

        return await response.Content.ReadFromJsonAsync<MissionTemplate>();
    }

    public async Task<MissionTemplate?> UpdateMissionTemplateAsync(Guid id, UpdateMissionTemplateRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/mission-templates/{id}", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }

        return await response.Content.ReadFromJsonAsync<MissionTemplate>();
    }

    public async Task DeleteMissionTemplateAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/mission-templates/{id}");

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }
    }

    // ObjectiveDimension methods
    public async Task<PagedResult<ObjectiveDimension>?> GetObjectiveDimensionsAsync(string? search, int page = 1, int pageSize = 10)
    {
        (page, pageSize) = NormalizePagination(page, pageSize);

        var queryParams = new List<string>();

        if (!string.IsNullOrEmpty(search))
        {
            queryParams.Add($"search={Uri.EscapeDataString(search)}");
        }

        queryParams.Add($"page={page}");
        queryParams.Add($"pageSize={pageSize}");

        var url = $"api/objective-dimensions?{string.Join("&", queryParams)}";
        return await GetSafeAsync<PagedResult<ObjectiveDimension>>(url);
    }

    public async Task<ObjectiveDimension?> CreateObjectiveDimensionAsync(CreateObjectiveDimensionRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/objective-dimensions", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }

        return await response.Content.ReadFromJsonAsync<ObjectiveDimension>();
    }

    public async Task<ObjectiveDimension?> UpdateObjectiveDimensionAsync(Guid id, UpdateObjectiveDimensionRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/objective-dimensions/{id}", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }

        return await response.Content.ReadFromJsonAsync<ObjectiveDimension>();
    }

    public async Task DeleteObjectiveDimensionAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/objective-dimensions/{id}");

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }
    }

    private async Task<T?> GetSafeAsync<T>(string url)
    {
        try
        {
            return await _http.GetFromJsonAsync<T>(url);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao carregar dados ({url}): {ex.Message}");
            ShowLoadWarningThrottled();
            return default;
        }
    }

    private void ShowLoadWarningThrottled()
    {
        if ((DateTime.UtcNow - _lastLoadWarningUtc).TotalSeconds < 3)
        {
            return;
        }

        _lastLoadWarningUtc = DateTime.UtcNow;
        _toastService.ShowWarning("Falha ao carregar dados",
            "Não foi possível carregar os dados. Verifique sua conexão e tente novamente.");
    }

    private static async Task<string> ExtractErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            string body = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(body);
            JsonElement root = doc.RootElement;

            // ValidationProblemDetails — extract messages from "errors" dict
            if (root.TryGetProperty("errors", out JsonElement errors))
            {
                List<string> messages = new();
                foreach (JsonProperty field in errors.EnumerateObject())
                {
                    foreach (JsonElement msg in field.Value.EnumerateArray())
                    {
                        messages.Add(msg.GetString() ?? string.Empty);
                    }
                }

                if (messages.Count > 0)
                {
                    return string.Join(" ", messages);
                }
            }

            // ProblemDetails — extract "detail"
            if (root.TryGetProperty("detail", out JsonElement detail))
            {
                string? detailText = detail.GetString();
                if (!string.IsNullOrWhiteSpace(detailText))
                {
                    return detailText;
                }
            }
        }
        catch
        {
            // Fallback if response body can't be parsed
        }

        return $"Erro do servidor ({(int)response.StatusCode}).";
    }

    public async Task<AuthLoginResponse?> LoginAsync(AuthLoginRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AuthLoginResponse>();
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound ||
            response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return null;
        }

        var errorMessage = await ExtractErrorMessageAsync(response);
        throw new HttpRequestException(errorMessage);
    }

    public async Task LogoutAsync()
    {
        var response = await _http.PostAsync("api/auth/logout", null);
        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }
    }

    // Collaborator Subordinates (Hierarchy)
    public async Task<List<CollaboratorHierarchyNodeDto>?> GetCollaboratorSubordinatesAsync(Guid collaboratorId)
    {
        return await GetSafeAsync<List<CollaboratorHierarchyNodeDto>>($"api/collaborators/{collaboratorId}/subordinates");
    }

    // Collaborator Teams (Many-to-Many)
    public async Task<List<TeamSummaryDto>?> GetCollaboratorTeamsAsync(Guid collaboratorId)
    {
        return await GetSafeAsync<List<TeamSummaryDto>>($"api/collaborators/{collaboratorId}/teams");
    }

    public async Task UpdateCollaboratorTeamsAsync(Guid collaboratorId, UpdateCollaboratorTeamsRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/collaborators/{collaboratorId}/teams", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }
    }

    public async Task<List<TeamSummaryDto>?> GetAvailableTeamsForCollaboratorAsync(Guid collaboratorId, string? search = null)
    {
        var url = $"api/collaborators/{collaboratorId}/available-teams";
        if (!string.IsNullOrWhiteSpace(search))
        {
            url += $"?search={Uri.EscapeDataString(search)}";
        }
        return await GetSafeAsync<List<TeamSummaryDto>>(url);
    }

    // Collaborator Summaries
    public async Task<List<CollaboratorSummaryDto>?> GetCollaboratorSummariesAsync(string? search = null)
    {
        var url = "api/collaborators/summaries";
        if (!string.IsNullOrWhiteSpace(search))
        {
            url += $"?search={Uri.EscapeDataString(search)}";
        }
        return await GetSafeAsync<List<CollaboratorSummaryDto>>(url);
    }

    // Team Collaborators (Many-to-Many)
    public async Task<List<CollaboratorSummaryDto>?> GetTeamCollaboratorSummariesAsync(Guid teamId)
    {
        return await GetSafeAsync<List<CollaboratorSummaryDto>>($"api/teams/{teamId}/collaborators-summary");
    }

    public async Task UpdateTeamCollaboratorsAsync(Guid teamId, UpdateTeamCollaboratorsRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/teams/{teamId}/collaborators", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }
    }

    public async Task<List<CollaboratorSummaryDto>?> GetAvailableCollaboratorsForTeamAsync(Guid teamId, string? search = null)
    {
        var url = $"api/teams/{teamId}/available-collaborators";
        if (!string.IsNullOrWhiteSpace(search))
        {
            url += $"?search={Uri.EscapeDataString(search)}";
        }
        return await GetSafeAsync<List<CollaboratorSummaryDto>>(url);
    }

    // Notification methods
    public async Task<PagedResult<NotificationDto>?> GetNotificationsAsync(int page = 1, int pageSize = 20)
    {
        (page, pageSize) = NormalizePagination(page, pageSize);
        var url = $"api/notifications?page={page}&pageSize={pageSize}";
        return await GetSafeAsync<PagedResult<NotificationDto>>(url);
    }

    public async Task<UnreadCountResponse?> GetNotificationUnreadCountAsync()
    {
        return await GetSafeAsync<UnreadCountResponse>("api/notifications/unread-count");
    }

    public async Task MarkNotificationAsReadAsync(Guid id)
    {
        var response = await _http.PutAsync($"api/notifications/{id}/read", null);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }
    }

    public async Task MarkAllNotificationsAsReadAsync()
    {
        var response = await _http.PutAsync("api/notifications/read-all", null);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }
    }

    private static (int Page, int PageSize) NormalizePagination(int page, int pageSize)
    {
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        return (normalizedPage, normalizedPageSize);
    }
}
