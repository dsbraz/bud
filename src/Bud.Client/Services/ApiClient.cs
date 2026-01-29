using System.Net.Http.Json;
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

    public async Task<PagedResult<Organization>?> GetOrganizationsAsync(string? search, int page = 1, int pageSize = 10)
    {
        var url = $"api/organizations?search={Uri.EscapeDataString(search ?? string.Empty)}&page={page}&pageSize={pageSize}";
        return await _http.GetFromJsonAsync<PagedResult<Organization>>(url);
    }

    public async Task<Organization?> CreateOrganizationAsync(CreateOrganizationRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/organizations", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Organization>();
    }

    public async Task<List<LeaderCollaboratorResponse>?> GetLeadersAsync()
    {
        return await _http.GetFromJsonAsync<List<LeaderCollaboratorResponse>>("api/collaborators/leaders");
    }

    public async Task<PagedResult<Workspace>?> GetWorkspacesAsync(Guid? organizationId, string? search, int page = 1, int pageSize = 10)
    {
        var orgParam = organizationId.HasValue ? organizationId.Value.ToString() : string.Empty;
        var url = $"api/workspaces?organizationId={Uri.EscapeDataString(orgParam)}&search={Uri.EscapeDataString(search ?? string.Empty)}&page={page}&pageSize={pageSize}";
        return await _http.GetFromJsonAsync<PagedResult<Workspace>>(url);
    }

    public async Task<Workspace?> CreateWorkspaceAsync(CreateWorkspaceRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/workspaces", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Workspace>();
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
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Team>();
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
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Collaborator>();
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
}
