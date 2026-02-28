using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bud.Mcp.Auth;
using Bud.Shared.Contracts;

namespace Bud.Mcp.Http;

public sealed class BudApiClient(HttpClient httpClient, BudApiSession session)
{
    private static readonly JsonSerializerOptions RequestJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly JsonSerializerOptions ResponseJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _httpClient = httpClient;
    private readonly BudApiSession _session = session;

    public Task<GoalResponse> CreateGoalAsync(CreateGoalRequest request, CancellationToken cancellationToken = default)
        => PostAsync<CreateGoalRequest, GoalResponse>("/api/goals", request, cancellationToken);

    public Task<GoalResponse> GetGoalAsync(Guid id, CancellationToken cancellationToken = default)
        => GetAsync<GoalResponse>($"/api/goals/{id}", cancellationToken);

    public Task<PagedResult<GoalResponse>> ListGoalsAsync(GoalScopeType? scopeType, Guid? scopeId, string? search, int page, int pageSize, CancellationToken cancellationToken = default)
        => GetAsync<PagedResult<GoalResponse>>(BuildQueryPath(
            "/api/goals",
            ("scopeType", scopeType?.ToString()),
            ("scopeId", scopeId?.ToString()),
            ("search", search),
            ("page", page.ToString(CultureInfo.InvariantCulture)),
            ("pageSize", pageSize.ToString(CultureInfo.InvariantCulture))), cancellationToken);

    public Task<GoalResponse> UpdateGoalAsync(Guid id, PatchGoalRequest request, CancellationToken cancellationToken = default)
        => PatchAsync<PatchGoalRequest, GoalResponse>($"/api/goals/{id}", request, cancellationToken);

    public Task DeleteGoalAsync(Guid id, CancellationToken cancellationToken = default)
        => DeleteAsync($"/api/goals/{id}", cancellationToken);

    public Task<IndicatorResponse> CreateGoalIndicatorAsync(CreateIndicatorRequest request, CancellationToken cancellationToken = default)
        => PostAsync<CreateIndicatorRequest, IndicatorResponse>("/api/indicators", request, cancellationToken);

    public Task<IndicatorResponse> GetGoalIndicatorAsync(Guid id, CancellationToken cancellationToken = default)
        => GetAsync<IndicatorResponse>($"/api/indicators/{id}", cancellationToken);

    public Task<PagedResult<IndicatorResponse>> ListGoalIndicatorsAsync(Guid? goalId, string? search, int page, int pageSize, CancellationToken cancellationToken = default)
        => GetAsync<PagedResult<IndicatorResponse>>(BuildQueryPath(
            "/api/indicators",
            ("goalId", goalId?.ToString()),
            ("search", search),
            ("page", page.ToString(CultureInfo.InvariantCulture)),
            ("pageSize", pageSize.ToString(CultureInfo.InvariantCulture))), cancellationToken);

    public Task<IndicatorResponse> UpdateGoalIndicatorAsync(Guid id, PatchIndicatorRequest request, CancellationToken cancellationToken = default)
        => PatchAsync<PatchIndicatorRequest, IndicatorResponse>($"/api/indicators/{id}", request, cancellationToken);

    public Task DeleteGoalIndicatorAsync(Guid id, CancellationToken cancellationToken = default)
        => DeleteAsync($"/api/indicators/{id}", cancellationToken);

    public Task<CheckinResponse> CreateIndicatorCheckinAsync(Guid indicatorId, CreateCheckinRequest request, CancellationToken cancellationToken = default)
        => PostAsync<CreateCheckinRequest, CheckinResponse>($"/api/indicators/{indicatorId}/checkins", request, cancellationToken);

    public Task<CheckinResponse> GetIndicatorCheckinAsync(Guid indicatorId, Guid id, CancellationToken cancellationToken = default)
        => GetAsync<CheckinResponse>($"/api/indicators/{indicatorId}/checkins/{id}", cancellationToken);

    public Task<PagedResult<CheckinResponse>> ListIndicatorCheckinsAsync(Guid indicatorId, int page, int pageSize, CancellationToken cancellationToken = default)
        => GetAsync<PagedResult<CheckinResponse>>(BuildQueryPath(
            $"/api/indicators/{indicatorId}/checkins",
            ("page", page.ToString(CultureInfo.InvariantCulture)),
            ("pageSize", pageSize.ToString(CultureInfo.InvariantCulture))), cancellationToken);

    public Task<CheckinResponse> UpdateIndicatorCheckinAsync(Guid indicatorId, Guid id, PatchCheckinRequest request, CancellationToken cancellationToken = default)
        => PatchAsync<PatchCheckinRequest, CheckinResponse>($"/api/indicators/{indicatorId}/checkins/{id}", request, cancellationToken);

    public Task DeleteIndicatorCheckinAsync(Guid indicatorId, Guid id, CancellationToken cancellationToken = default)
        => DeleteAsync($"/api/indicators/{indicatorId}/checkins/{id}", cancellationToken);

    private async Task<TResponse> GetAsync<TResponse>(string path, CancellationToken cancellationToken)
    {
        using var request = _session.CreateDomainRequest(HttpMethod.Get, path);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        return await ReadSuccessResponseOrThrowAsync<TResponse>(response, cancellationToken);
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest payload, CancellationToken cancellationToken)
    {
        using var request = _session.CreateDomainRequest(HttpMethod.Post, path);
        request.Content = JsonContent.Create(payload, options: RequestJsonOptions);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        return await ReadSuccessResponseOrThrowAsync<TResponse>(response, cancellationToken);
    }

    private async Task<TResponse> PatchAsync<TRequest, TResponse>(string path, TRequest payload, CancellationToken cancellationToken)
    {
        using var request = _session.CreateDomainRequest(HttpMethod.Patch, path);
        request.Content = JsonContent.Create(payload, options: RequestJsonOptions);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        return await ReadSuccessResponseOrThrowAsync<TResponse>(response, cancellationToken);
    }

    private async Task DeleteAsync(string path, CancellationToken cancellationToken)
    {
        using var request = _session.CreateDomainRequest(HttpMethod.Delete, path);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw await BudApiException.FromHttpResponseAsync(response, cancellationToken);
        }
    }

    private static async Task<T> ReadSuccessResponseOrThrowAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw await BudApiException.FromHttpResponseAsync(response, cancellationToken);
        }

        var payload = await response.Content.ReadFromJsonAsync<T>(ResponseJsonOptions, cancellationToken);
        if (payload is null)
        {
            throw new InvalidOperationException("Resposta da API Bud inválida ou vazia.");
        }

        return payload;
    }

    private static string BuildQueryPath(string basePath, params (string Name, string? Value)[] parameters)
    {
        var query = new StringBuilder();
        foreach (var (name, value) in parameters)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            query.Append(query.Length == 0 ? '?' : '&');
            query.Append(Uri.EscapeDataString(name));
            query.Append('=');
            query.Append(Uri.EscapeDataString(value));
        }

        return $"{basePath}{query}";
    }
}
