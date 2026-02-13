using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bud.Mcp.Auth;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

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

    public Task<Mission> CreateMissionAsync(CreateMissionRequest request, CancellationToken cancellationToken = default)
        => PostAsync<CreateMissionRequest, Mission>("/api/missions", request, cancellationToken);

    public Task<Mission> GetMissionAsync(Guid id, CancellationToken cancellationToken = default)
        => GetAsync<Mission>($"/api/missions/{id}", cancellationToken);

    public Task<PagedResult<Mission>> ListMissionsAsync(MissionScopeType? scopeType, Guid? scopeId, string? search, int page, int pageSize, CancellationToken cancellationToken = default)
        => GetAsync<PagedResult<Mission>>(BuildQueryPath(
            "/api/missions",
            ("scopeType", scopeType?.ToString()),
            ("scopeId", scopeId?.ToString()),
            ("search", search),
            ("page", page.ToString(CultureInfo.InvariantCulture)),
            ("pageSize", pageSize.ToString(CultureInfo.InvariantCulture))), cancellationToken);

    public Task<Mission> UpdateMissionAsync(Guid id, UpdateMissionRequest request, CancellationToken cancellationToken = default)
        => PutAsync<UpdateMissionRequest, Mission>($"/api/missions/{id}", request, cancellationToken);

    public Task DeleteMissionAsync(Guid id, CancellationToken cancellationToken = default)
        => DeleteAsync($"/api/missions/{id}", cancellationToken);

    public Task<MissionMetric> CreateMissionMetricAsync(CreateMissionMetricRequest request, CancellationToken cancellationToken = default)
        => PostAsync<CreateMissionMetricRequest, MissionMetric>("/api/mission-metrics", request, cancellationToken);

    public Task<MissionMetric> GetMissionMetricAsync(Guid id, CancellationToken cancellationToken = default)
        => GetAsync<MissionMetric>($"/api/mission-metrics/{id}", cancellationToken);

    public Task<PagedResult<MissionMetric>> ListMissionMetricsAsync(Guid? missionId, string? search, int page, int pageSize, CancellationToken cancellationToken = default)
        => GetAsync<PagedResult<MissionMetric>>(BuildQueryPath(
            "/api/mission-metrics",
            ("missionId", missionId?.ToString()),
            ("search", search),
            ("page", page.ToString(CultureInfo.InvariantCulture)),
            ("pageSize", pageSize.ToString(CultureInfo.InvariantCulture))), cancellationToken);

    public Task<MissionMetric> UpdateMissionMetricAsync(Guid id, UpdateMissionMetricRequest request, CancellationToken cancellationToken = default)
        => PutAsync<UpdateMissionMetricRequest, MissionMetric>($"/api/mission-metrics/{id}", request, cancellationToken);

    public Task DeleteMissionMetricAsync(Guid id, CancellationToken cancellationToken = default)
        => DeleteAsync($"/api/mission-metrics/{id}", cancellationToken);

    public Task<MetricCheckin> CreateMetricCheckinAsync(CreateMetricCheckinRequest request, CancellationToken cancellationToken = default)
        => PostAsync<CreateMetricCheckinRequest, MetricCheckin>("/api/metric-checkins", request, cancellationToken);

    public Task<MetricCheckin> GetMetricCheckinAsync(Guid id, CancellationToken cancellationToken = default)
        => GetAsync<MetricCheckin>($"/api/metric-checkins/{id}", cancellationToken);

    public Task<PagedResult<MetricCheckin>> ListMetricCheckinsAsync(Guid? missionMetricId, Guid? missionId, int page, int pageSize, CancellationToken cancellationToken = default)
        => GetAsync<PagedResult<MetricCheckin>>(BuildQueryPath(
            "/api/metric-checkins",
            ("missionMetricId", missionMetricId?.ToString()),
            ("missionId", missionId?.ToString()),
            ("page", page.ToString(CultureInfo.InvariantCulture)),
            ("pageSize", pageSize.ToString(CultureInfo.InvariantCulture))), cancellationToken);

    public Task<MetricCheckin> UpdateMetricCheckinAsync(Guid id, UpdateMetricCheckinRequest request, CancellationToken cancellationToken = default)
        => PutAsync<UpdateMetricCheckinRequest, MetricCheckin>($"/api/metric-checkins/{id}", request, cancellationToken);

    public Task DeleteMetricCheckinAsync(Guid id, CancellationToken cancellationToken = default)
        => DeleteAsync($"/api/metric-checkins/{id}", cancellationToken);

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

    private async Task<TResponse> PutAsync<TRequest, TResponse>(string path, TRequest payload, CancellationToken cancellationToken)
    {
        using var request = _session.CreateDomainRequest(HttpMethod.Put, path);
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
