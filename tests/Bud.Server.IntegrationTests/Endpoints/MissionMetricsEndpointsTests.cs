using System.Net;
using System.Net.Http.Json;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bud.Server.IntegrationTests.Endpoints;

public class MissionMetricsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public MissionMetricsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateGlobalAdminClient();
    }

    private async Task<Guid> GetOrCreateAdminLeader()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Bud.Server.Data.ApplicationDbContext>();

        var existingLeader = await dbContext.Collaborators
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Email == "admin@getbud.co");

        if (existingLeader != null)
        {
            return existingLeader.Id;
        }

        var org = new Organization { Id = Guid.NewGuid(), Name = "getbud.co", OwnerId = null };
        dbContext.Organizations.Add(org);

        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "getbud.co", OrganizationId = org.Id };
        dbContext.Workspaces.Add(workspace);

        var team = new Team { Id = Guid.NewGuid(), Name = "getbud.co", WorkspaceId = workspace.Id, OrganizationId = org.Id };
        dbContext.Teams.Add(team);

        var adminLeader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Administrador",
            Email = "admin@getbud.co",
            Role = CollaboratorRole.Leader,
            TeamId = team.Id,
            OrganizationId = org.Id
        };
        dbContext.Collaborators.Add(adminLeader);

        await dbContext.SaveChangesAsync();

        org.OwnerId = adminLeader.Id;
        await dbContext.SaveChangesAsync();

        return adminLeader.Id;
    }

    private async Task<Mission> CreateTestMission()
    {
        var leaderId = await GetOrCreateAdminLeader();
        var orgResponse = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest
            {
                Name = "test-org.com",
                OwnerId = leaderId,
            });
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
        metric.QuantitativeType.Should().BeNull();
        metric.MinValue.Should().BeNull();
        metric.MaxValue.Should().BeNull();
        metric.Unit.Should().BeNull();
    }

    [Fact]
    public async Task Create_WithQuantitativeMetricKeepAbove_ReturnsCreated()
    {
        // Arrange
        var mission = await CreateTestMission();

        var request = new CreateMissionMetricRequest
        {
            MissionId = mission.Id,
            Name = "Story Points",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.KeepAbove,
            MinValue = 50m,
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
        metric.QuantitativeType.Should().Be(QuantitativeMetricType.KeepAbove);
        metric.MinValue.Should().Be(50m);
        metric.MaxValue.Should().BeNull();
        metric.Unit.Should().Be(MetricUnit.Points);
        metric.TargetText.Should().BeNull();
    }

    [Fact]
    public async Task Create_WithQuantitativeMetricKeepBelow_ReturnsCreated()
    {
        // Arrange
        var mission = await CreateTestMission();

        var request = new CreateMissionMetricRequest
        {
            MissionId = mission.Id,
            Name = "Error Rate",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.KeepBelow,
            MaxValue = 5m,
            Unit = MetricUnit.Percentage
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/mission-metrics", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var metric = await response.Content.ReadFromJsonAsync<MissionMetric>();
        metric.Should().NotBeNull();
        metric!.Name.Should().Be("Error Rate");
        metric.Type.Should().Be(MetricType.Quantitative);
        metric.QuantitativeType.Should().Be(QuantitativeMetricType.KeepBelow);
        metric.MinValue.Should().BeNull();
        metric.MaxValue.Should().Be(5m);
        metric.Unit.Should().Be(MetricUnit.Percentage);
        metric.TargetText.Should().BeNull();
    }

    [Fact]
    public async Task Create_WithQuantitativeMetricKeepBetween_ReturnsCreated()
    {
        // Arrange
        var mission = await CreateTestMission();

        var request = new CreateMissionMetricRequest
        {
            MissionId = mission.Id,
            Name = "Response Time",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.KeepBetween,
            MinValue = 100m,
            MaxValue = 500m,
            Unit = MetricUnit.Integer
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/mission-metrics", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var metric = await response.Content.ReadFromJsonAsync<MissionMetric>();
        metric.Should().NotBeNull();
        metric!.Name.Should().Be("Response Time");
        metric.Type.Should().Be(MetricType.Quantitative);
        metric.QuantitativeType.Should().Be(QuantitativeMetricType.KeepBetween);
        metric.MinValue.Should().Be(100m);
        metric.MaxValue.Should().Be(500m);
        metric.Unit.Should().Be(MetricUnit.Integer);
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

    [Fact]
    public async Task Create_WithQuantitativeMetricAchieve_ReturnsCreated()
    {
        // Arrange
        var mission = await CreateTestMission();

        var request = new CreateMissionMetricRequest
        {
            MissionId = mission.Id,
            Name = "Sales Target",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.Achieve,
            MaxValue = 100m,
            Unit = MetricUnit.Integer
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/mission-metrics", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var metric = await response.Content.ReadFromJsonAsync<MissionMetric>();
        metric.Should().NotBeNull();
        metric!.Name.Should().Be("Sales Target");
        metric.Type.Should().Be(MetricType.Quantitative);
        metric.QuantitativeType.Should().Be(QuantitativeMetricType.Achieve);
        metric.MinValue.Should().BeNull();
        metric.MaxValue.Should().Be(100m);
        metric.Unit.Should().Be(MetricUnit.Integer);
        metric.TargetText.Should().BeNull();
    }

    [Fact]
    public async Task Create_WithQuantitativeMetricReduce_ReturnsCreated()
    {
        // Arrange
        var mission = await CreateTestMission();

        var request = new CreateMissionMetricRequest
        {
            MissionId = mission.Id,
            Name = "Cost Reduction",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.Reduce,
            MaxValue = 50m,
            Unit = MetricUnit.Percentage
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/mission-metrics", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var metric = await response.Content.ReadFromJsonAsync<MissionMetric>();
        metric.Should().NotBeNull();
        metric!.Name.Should().Be("Cost Reduction");
        metric.Type.Should().Be(MetricType.Quantitative);
        metric.QuantitativeType.Should().Be(QuantitativeMetricType.Reduce);
        metric.MinValue.Should().BeNull();
        metric.MaxValue.Should().Be(50m);
        metric.Unit.Should().Be(MetricUnit.Percentage);
        metric.TargetText.Should().BeNull();
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
            QuantitativeType = QuantitativeMetricType.KeepBetween,
            MinValue = 50m,
            MaxValue = 100m,
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
        updated.QuantitativeType.Should().Be(QuantitativeMetricType.KeepBetween);
        updated.MinValue.Should().Be(50m);
        updated.MaxValue.Should().Be(100m);
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

    [Fact]
    public async Task GetAll_WithInvalidPageSize_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/mission-metrics?page=1&pageSize=101");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Detail.Should().Be("O parâmetro 'pageSize' deve estar entre 1 e 100.");
    }

    [Fact]
    public async Task GetAll_WithSearchTooLong_ReturnsBadRequest()
    {
        // Arrange
        var search = new string('a', 201);

        // Act
        var response = await _client.GetAsync($"/api/mission-metrics?search={search}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Detail.Should().Be("O parâmetro 'search' deve ter no máximo 200 caracteres.");
    }

    [Fact]
    public async Task GetProgress_WithInvalidIds_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/mission-metrics/progress?ids=abc");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Detail.Should().Be("O parâmetro 'ids' contém valores inválidos. Informe GUIDs separados por vírgula.");
    }

    [Fact]
    public async Task Create_WithTenantMismatch_ReturnsForbidden()
    {
        // Arrange
        var leaderId = await GetOrCreateAdminLeader();
        var org1Response = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest
            {
                Name = "metric-org-1.com",
                OwnerId = leaderId
            });
        var org1 = await org1Response.Content.ReadFromJsonAsync<Organization>();

        var org2Response = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest
            {
                Name = "metric-org-2.com",
                OwnerId = leaderId
            });
        var org2 = await org2Response.Content.ReadFromJsonAsync<Organization>();

        var missionResponse = await _client.PostAsJsonAsync("/api/missions",
            new CreateMissionRequest
            {
                Name = "Mission Org 2",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(7),
                Status = MissionStatus.Planned,
                ScopeType = MissionScopeType.Organization,
                ScopeId = org2!.Id
            });
        var mission = await missionResponse.Content.ReadFromJsonAsync<Mission>();

        var collaborator = await CreateNonOwnerCollaborator(org1!.Id);
        var tenantClient = _factory.CreateTenantClient(org1.Id, collaborator.Email, collaborator.Id);

        var request = new CreateMissionMetricRequest
        {
            MissionId = mission!.Id,
            Name = "Metric Forbidden",
            Type = MetricType.Qualitative,
            TargetText = "Teste"
        };

        // Act
        var response = await tenantClient.PostAsJsonAsync("/api/mission-metrics", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var unauthenticatedClient = _factory.CreateClient();

        // Act
        var response = await unauthenticatedClient.GetAsync("/api/mission-metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    private async Task<Collaborator> CreateNonOwnerCollaborator(Guid organizationId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Bud.Server.Data.ApplicationDbContext>();

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Colaborador Teste",
            Email = $"colaborador-{Guid.NewGuid():N}@test.com",
            Role = CollaboratorRole.IndividualContributor,
            OrganizationId = organizationId
        };

        dbContext.Collaborators.Add(collaborator);
        await dbContext.SaveChangesAsync();

        return collaborator;
    }
}
