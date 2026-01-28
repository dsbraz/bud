using System.Net;
using System.Net.Http.Json;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Xunit;

namespace Bud.Server.IntegrationTests.Endpoints;

public class MissionMetricsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MissionMetricsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<Mission> CreateTestMission()
    {
        var orgResponse = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest { Name = "Test Org" });
        var org = await orgResponse.Content.ReadFromJsonAsync<Organization>();

        var missionResponse = await _client.PostAsJsonAsync("/api/missions",
            new CreateMissionRequest
            {
                Name = "Test Mission",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(7),
                Status = MissionStatus.Planned,
                ScopeType = MissionScopeType.Organization,
                ScopeId = org!.Id
            });

        return (await missionResponse.Content.ReadFromJsonAsync<Mission>())!;
    }

    #region Create Tests

    [Fact]
    public async Task Create_WithQualitativeMetric_ReturnsCreated()
    {
        // Arrange
        var mission = await CreateTestMission();

        var request = new CreateMissionMetricRequest
        {
            MissionId = mission.Id,
            Name = "Quality Metric",
            Type = MetricType.Qualitative,
            TargetText = "Achieve high quality standards"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/mission-metrics", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var metric = await response.Content.ReadFromJsonAsync<MissionMetric>();
        metric.Should().NotBeNull();
        metric!.Name.Should().Be("Quality Metric");
        metric.Type.Should().Be(MetricType.Qualitative);
        metric.TargetText.Should().Be("Achieve high quality standards");
        metric.TargetValue.Should().BeNull();
        metric.Unit.Should().BeNull();
    }

    [Fact]
    public async Task Create_WithQuantitativeMetric_ReturnsCreated()
    {
        // Arrange
        var mission = await CreateTestMission();

        var request = new CreateMissionMetricRequest
        {
            MissionId = mission.Id,
            Name = "Story Points",
            Type = MetricType.Quantitative,
            TargetValue = 50m,
            Unit = MetricUnit.Points
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/mission-metrics", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var metric = await response.Content.ReadFromJsonAsync<MissionMetric>();
        metric.Should().NotBeNull();
        metric!.Name.Should().Be("Story Points");
        metric.Type.Should().Be(MetricType.Quantitative);
        metric.TargetValue.Should().Be(50m);
        metric.Unit.Should().Be(MetricUnit.Points);
        metric.TargetText.Should().BeNull();
    }

    [Fact]
    public async Task Create_WithInvalidMissionId_ReturnsNotFound()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(), // Non-existent mission
            Name = "Test Metric",
            Type = MetricType.Qualitative,
            TargetText = "Test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/mission-metrics", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ChangingMetricType_UpdatesCorrectly()
    {
        // Arrange: Create qualitative metric
        var mission = await CreateTestMission();

        var createRequest = new CreateMissionMetricRequest
        {
            MissionId = mission.Id,
            Name = "Original Metric",
            Type = MetricType.Qualitative,
            TargetText = "Original text"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/mission-metrics", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<MissionMetric>();

        // Update to quantitative
        var updateRequest = new UpdateMissionMetricRequest
        {
            Name = "Updated Metric",
            Type = MetricType.Quantitative,
            TargetValue = 100m,
            Unit = MetricUnit.Hours
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/mission-metrics/{created!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<MissionMetric>();
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Metric");
        updated.Type.Should().Be(MetricType.Quantitative);
        updated.TargetValue.Should().Be(100m);
        updated.Unit.Should().Be(MetricUnit.Hours);
        updated.TargetText.Should().BeNull(); // Should be cleared
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WithMissionIdFilter_ReturnsOnlyMissionMetrics()
    {
        // Arrange: Create two missions
        var mission1 = await CreateTestMission();
        var mission2 = await CreateTestMission();

        // Create metrics for each mission
        await _client.PostAsJsonAsync("/api/mission-metrics",
            new CreateMissionMetricRequest
            {
                MissionId = mission1.Id,
                Name = "Metric Mission 1",
                Type = MetricType.Qualitative,
                TargetText = "Test"
            });

        await _client.PostAsJsonAsync("/api/mission-metrics",
            new CreateMissionMetricRequest
            {
                MissionId = mission2.Id,
                Name = "Metric Mission 2",
                Type = MetricType.Qualitative,
                TargetText = "Test"
            });

        // Act - Filter by mission1
        var response = await _client.GetAsync($"/api/mission-metrics?missionId={mission1.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<MissionMetric>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items.Should().OnlyContain(m => m.MissionId == mission1.Id);
    }

    #endregion
}
