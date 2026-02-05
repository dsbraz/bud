using System.Net.Http.Json;
using System.Text.Json;
using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Client.Services;

public sealed class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<OrganizationSummaryDto>?> GetMyOrganizationsAsync()
    {
        return await _http.GetFromJsonAsync<List<OrganizationSummaryDto>>("api/auth/my-organizations");
    }

    public async Task<PagedResult<Organization>?> GetOrganizationsAsync(string? search, int page = 1, int pageSize = 10)
    {
        var url = $"api/organizations?search={Uri.EscapeDataString(search ?? string.Empty)}&page={page}&pageSize={pageSize}";
        return await _http.GetFromJsonAsync<PagedResult<Organization>>(url);
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
        return await _http.GetFromJsonAsync<List<LeaderCollaboratorResponse>>(url);
    }

    public async Task<PagedResult<Workspace>?> GetWorkspacesAsync(Guid? organizationId, string? search, int page = 1, int pageSize = 10)
    {
        var orgParam = organizationId.HasValue ? organizationId.Value.ToString() : string.Empty;
        var url = $"api/workspaces?organizationId={Uri.EscapeDataString(orgParam)}&search={Uri.EscapeDataString(search ?? string.Empty)}&page={page}&pageSize={pageSize}";
        return await _http.GetFromJsonAsync<PagedResult<Workspace>>(url);
    }

    public async Task<PagedResult<Collaborator>?> GetOrganizationCollaboratorsAsync(Guid organizationId, int page = 1, int pageSize = 10)
    {
        var url = $"api/organizations/{organizationId}/collaborators?page={page}&pageSize={pageSize}";
        return await _http.GetFromJsonAsync<PagedResult<Collaborator>>(url);
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
        var workspaceParam = workspaceId.HasValue ? workspaceId.Value.ToString() : string.Empty;
        var parentParam = parentTeamId.HasValue ? parentTeamId.Value.ToString() : string.Empty;
        var url = $"api/teams?workspaceId={Uri.EscapeDataString(workspaceParam)}&parentTeamId={Uri.EscapeDataString(parentParam)}&search={Uri.EscapeDataString(search ?? string.Empty)}&page={page}&pageSize={pageSize}";
        return await _http.GetFromJsonAsync<PagedResult<Team>>(url);
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
        var teamParam = teamId.HasValue ? teamId.Value.ToString() : string.Empty;
        var url = $"api/collaborators?teamId={Uri.EscapeDataString(teamParam)}&search={Uri.EscapeDataString(search ?? string.Empty)}&page={page}&pageSize={pageSize}";
        return await _http.GetFromJsonAsync<PagedResult<Collaborator>>(url);
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
        var scopeTypeParam = scopeType.HasValue ? scopeType.Value.ToString() : string.Empty;
        var scopeIdParam = scopeId.HasValue ? scopeId.Value.ToString() : string.Empty;
        var url =
            $"api/missions?scopeType={Uri.EscapeDataString(scopeTypeParam)}&scopeId={Uri.EscapeDataString(scopeIdParam)}&search={Uri.EscapeDataString(search ?? string.Empty)}&page={page}&pageSize={pageSize}";
        return await _http.GetFromJsonAsync<PagedResult<Mission>>(url);
    }

    public async Task<Mission?> CreateMissionAsync(CreateMissionRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/missions", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Mission>();
    }

    public async Task<PagedResult<Mission>?> GetMyThingsAsync(
        Guid collaboratorId,
        string? search,
        int page = 1,
        int pageSize = 10)
    {
        var url = $"api/missions/my-missions/{collaboratorId}?search={Uri.EscapeDataString(search ?? string.Empty)}&page={page}&pageSize={pageSize}";
        return await _http.GetFromJsonAsync<PagedResult<Mission>>(url);
    }

    public async Task<PagedResult<MissionMetric>?> GetMissionMetricsAsync(Guid? missionId, string? search, int page = 1, int pageSize = 10)
    {
        var missionParam = missionId.HasValue ? missionId.Value.ToString() : string.Empty;
        var url =
            $"api/mission-metrics?missionId={Uri.EscapeDataString(missionParam)}&search={Uri.EscapeDataString(search ?? string.Empty)}&page={page}&pageSize={pageSize}";
        return await _http.GetFromJsonAsync<PagedResult<MissionMetric>>(url);
    }

    public async Task<MissionMetric?> CreateMissionMetricAsync(CreateMissionMetricRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/mission-metrics", request);
        response.EnsureSuccessStatusCode();
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
        var url = $"api/missions/{missionId}/metrics?page={page}&pageSize={pageSize}";
        return await _http.GetFromJsonAsync<PagedResult<MissionMetric>>(url);
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

        response.EnsureSuccessStatusCode();
        return null;
    }

    public async Task LogoutAsync()
    {
        var response = await _http.PostAsync("api/auth/logout", null);
        response.EnsureSuccessStatusCode();
    }

    // Collaborator Teams (Many-to-Many)
    public async Task<List<TeamSummaryDto>?> GetCollaboratorTeamsAsync(Guid collaboratorId)
    {
        return await _http.GetFromJsonAsync<List<TeamSummaryDto>>($"api/collaborators/{collaboratorId}/teams");
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
        return await _http.GetFromJsonAsync<List<TeamSummaryDto>>(url);
    }

    // Team Collaborators (Many-to-Many)
    public async Task<List<CollaboratorSummaryDto>?> GetTeamCollaboratorSummariesAsync(Guid teamId)
    {
        return await _http.GetFromJsonAsync<List<CollaboratorSummaryDto>>($"api/teams/{teamId}/collaborators-summary");
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
        return await _http.GetFromJsonAsync<List<CollaboratorSummaryDto>>(url);
    }
}
