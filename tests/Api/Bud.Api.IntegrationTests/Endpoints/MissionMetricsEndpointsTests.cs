using System.Net;
using System.Net.Http.Json;
using Bud.Shared.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bud.Api.IntegrationTests.Endpoints;

public class MissionMetricsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public MissionMetricsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateGlobalAdminClient();
    }

    private void SetTenantHeader(Guid orgId)
    {
        _client.DefaultRequestHeaders.Remove("X-Tenant-Id");
        _client.DefaultRequestHeaders.Add("X-Tenant-Id", orgId.ToString());
    }

    private async Task<Guid> GetOrCreateAdminLeader()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Bud.Infrastructure.Persistence.ApplicationDbContext>();

        var existingLeader = await dbContext.Collaborators
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Email == "admin@getbud.co");

        if (existingLeader != null)
        {
            SetTenantHeader(existingLeader.OrganizationId);
            return existingLeader.Id;
        }

        var org = new Organization { Id = Guid.NewGuid(), Name = "getbud.co", OwnerId = null };
        dbContext.Organizations.Add(org);

        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "getbud.co", OrganizationId = org.Id };
        dbContext.Workspaces.Add(workspace);

        var adminLeader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Administrador",
            Email = "admin@getbud.co",
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id
        };
        dbContext.Collaborators.Add(adminLeader);

        var team = new Team { Id = Guid.NewGuid(), Name = "getbud.co", WorkspaceId = workspace.Id, OrganizationId = org.Id, LeaderId = adminLeader.Id };
        dbContext.Teams.Add(team);

        await dbContext.SaveChangesAsync();

        adminLeader.TeamId = team.Id;
        org.OwnerId = adminLeader.Id;
        await dbContext.SaveChangesAsync();

        SetTenantHeader(org.Id);
        return adminLeader.Id;
    }

    private async Task<Goal> CreateTestMission()
    {
        await GetOrCreateAdminLeader();
        var missionResponse = await _client.PostAsJsonAsync("/api/goals",
            new CreateGoalRequest
            {
                Name = "Test Mission",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(7),
                Status = Bud.Shared.Kernel.Enums.GoalStatus.Planned,
            });

        return (await missionResponse.Content.ReadFromJsonAsync<Goal>())!;
    }

    #region Create Tests

    [Fact]
    public async Task Create_WithQualitativeMetric_ReturnsCreated()
    {
        // Arrange
        var mission = await CreateTestMission();

        var request = new CreateIndicatorRequest
        {
            GoalId = mission.Id,
            Name = "Quality Metric",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Qualitative,
            TargetText = "Achieve high quality standards"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/indicators", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var indicator = await response.Content.ReadFromJsonAsync<Indicator>();
        indicator.Should().NotBeNull();
        indicator!.Name.Should().Be("Quality Metric");
        indicator.Type.Should().Be(IndicatorType.Qualitative);
        indicator.TargetText.Should().Be("Achieve high quality standards");
        indicator.QuantitativeType.Should().BeNull();
        indicator.MinValue.Should().BeNull();
        indicator.MaxValue.Should().BeNull();
        indicator.Unit.Should().BeNull();
    }

    [Fact]
    public async Task Create_WithQuantitativeMetricKeepAbove_ReturnsCreated()
    {
        // Arrange
        var mission = await CreateTestMission();

        var request = new CreateIndicatorRequest
        {
            GoalId = mission.Id,
            Name = "Story Points",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.KeepAbove,
            MinValue = 50m,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Points
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/indicators", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var indicator = await response.Content.ReadFromJsonAsync<Indicator>();
        indicator.Should().NotBeNull();
        indicator!.Name.Should().Be("Story Points");
        indicator.Type.Should().Be(IndicatorType.Quantitative);
        indicator.QuantitativeType.Should().Be(QuantitativeIndicatorType.KeepAbove);
        indicator.MinValue.Should().Be(50m);
        indicator.MaxValue.Should().BeNull();
        indicator.Unit.Should().Be(IndicatorUnit.Points);
        indicator.TargetText.Should().BeNull();
    }

    [Fact]
    public async Task Create_WithQuantitativeMetricKeepBelow_ReturnsCreated()
    {
        // Arrange
        var mission = await CreateTestMission();

        var request = new CreateIndicatorRequest
        {
            GoalId = mission.Id,
            Name = "Error Rate",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.KeepBelow,
            MaxValue = 5m,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Percentage
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/indicators", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var indicator = await response.Content.ReadFromJsonAsync<Indicator>();
        indicator.Should().NotBeNull();
        indicator!.Name.Should().Be("Error Rate");
        indicator.Type.Should().Be(IndicatorType.Quantitative);
        indicator.QuantitativeType.Should().Be(QuantitativeIndicatorType.KeepBelow);
        indicator.MinValue.Should().BeNull();
        indicator.MaxValue.Should().Be(5m);
        indicator.Unit.Should().Be(IndicatorUnit.Percentage);
        indicator.TargetText.Should().BeNull();
    }

    [Fact]
    public async Task Create_WithQuantitativeMetricKeepBetween_ReturnsCreated()
    {
        // Arrange
        var mission = await CreateTestMission();

        var request = new CreateIndicatorRequest
        {
            GoalId = mission.Id,
            Name = "Response Time",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.KeepBetween,
            MinValue = 100m,
            MaxValue = 500m,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Integer
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/indicators", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var indicator = await response.Content.ReadFromJsonAsync<Indicator>();
        indicator.Should().NotBeNull();
        indicator!.Name.Should().Be("Response Time");
        indicator.Type.Should().Be(IndicatorType.Quantitative);
        indicator.QuantitativeType.Should().Be(QuantitativeIndicatorType.KeepBetween);
        indicator.MinValue.Should().Be(100m);
        indicator.MaxValue.Should().Be(500m);
        indicator.Unit.Should().Be(IndicatorUnit.Integer);
        indicator.TargetText.Should().BeNull();
    }

    [Fact]
    public async Task Create_WithInvalidMissionId_ReturnsNotFound()
    {
        // Arrange
        var request = new CreateIndicatorRequest
        {
            GoalId = Guid.NewGuid(), // Non-existent goal
            Name = "Test Metric",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Qualitative,
            TargetText = "Test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/indicators", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithQuantitativeMetricAchieve_ReturnsCreated()
    {
        // Arrange
        var mission = await CreateTestMission();

        var request = new CreateIndicatorRequest
        {
            GoalId = mission.Id,
            Name = "Sales Target",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.Achieve,
            MaxValue = 100m,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Integer
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/indicators", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var indicator = await response.Content.ReadFromJsonAsync<Indicator>();
        indicator.Should().NotBeNull();
        indicator!.Name.Should().Be("Sales Target");
        indicator.Type.Should().Be(IndicatorType.Quantitative);
        indicator.QuantitativeType.Should().Be(QuantitativeIndicatorType.Achieve);
        indicator.MinValue.Should().BeNull();
        indicator.MaxValue.Should().Be(100m);
        indicator.Unit.Should().Be(IndicatorUnit.Integer);
        indicator.TargetText.Should().BeNull();
    }

    [Fact]
    public async Task Create_WithQuantitativeMetricReduce_ReturnsCreated()
    {
        // Arrange
        var mission = await CreateTestMission();

        var request = new CreateIndicatorRequest
        {
            GoalId = mission.Id,
            Name = "Cost Reduction",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.Reduce,
            MaxValue = 50m,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Percentage
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/indicators", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var indicator = await response.Content.ReadFromJsonAsync<Indicator>();
        indicator.Should().NotBeNull();
        indicator!.Name.Should().Be("Cost Reduction");
        indicator.Type.Should().Be(IndicatorType.Quantitative);
        indicator.QuantitativeType.Should().Be(QuantitativeIndicatorType.Reduce);
        indicator.MinValue.Should().BeNull();
        indicator.MaxValue.Should().Be(50m);
        indicator.Unit.Should().Be(IndicatorUnit.Percentage);
        indicator.TargetText.Should().BeNull();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ChangingMetricType_UpdatesCorrectly()
    {
        // Arrange: Create qualitative metric
        var mission = await CreateTestMission();

        var createRequest = new CreateIndicatorRequest
        {
            GoalId = mission.Id,
            Name = "Original Metric",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Qualitative,
            TargetText = "Original text"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/indicators", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<Indicator>();

        // Update to quantitative
        var updateRequest = new PatchIndicatorRequest
        {
            Name = "Updated Metric",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.KeepBetween,
            MinValue = 50m,
            MaxValue = 100m,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Hours
        };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/indicators/{created!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<Indicator>();
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Metric");
        updated.Type.Should().Be(IndicatorType.Quantitative);
        updated.QuantitativeType.Should().Be(QuantitativeIndicatorType.KeepBetween);
        updated.MinValue.Should().Be(50m);
        updated.MaxValue.Should().Be(100m);
        updated.Unit.Should().Be(IndicatorUnit.Hours);
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
        await _client.PostAsJsonAsync("/api/indicators",
            new CreateIndicatorRequest
            {
                GoalId = mission1.Id,
                Name = "Metric Mission 1",
                Type = Bud.Shared.Kernel.Enums.IndicatorType.Qualitative,
                TargetText = "Test"
            });

        await _client.PostAsJsonAsync("/api/indicators",
            new CreateIndicatorRequest
            {
                GoalId = mission2.Id,
                Name = "Metric Mission 2",
                Type = Bud.Shared.Kernel.Enums.IndicatorType.Qualitative,
                TargetText = "Test"
            });

        // Act - Filter by mission1
        var response = await _client.GetAsync($"/api/indicators?goalId={mission1.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<Indicator>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items.Should().OnlyContain(m => m.GoalId == mission1.Id);
    }

    [Fact]
    public async Task GetAll_WithInvalidPageSize_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/indicators?page=1&pageSize=101");

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
        var response = await _client.GetAsync($"/api/indicators?search={search}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Detail.Should().Be("O parâmetro 'search' deve ter no máximo 200 caracteres.");
    }

    [Fact]
    public async Task GetProgress_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/indicators/{Guid.NewGuid()}/progress");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithTenantMismatch_ReturnsNotFound()
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

        var missionResponse = await _client.PostAsJsonAsync("/api/goals",
            new CreateGoalRequest
            {
                Name = "Mission Org 2",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(7),
                Status = Bud.Shared.Kernel.Enums.GoalStatus.Planned,
            });
        var mission = await missionResponse.Content.ReadFromJsonAsync<Goal>();

        var collaborator = await CreateNonOwnerCollaborator(org1!.Id);
        var tenantClient = _factory.CreateTenantClient(org1.Id, collaborator.Email, collaborator.Id);

        var request = new CreateIndicatorRequest
        {
            GoalId = mission!.Id,
            Name = "Metric Forbidden",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Qualitative,
            TargetText = "Teste"
        };

        // Act
        var response = await tenantClient.PostAsJsonAsync("/api/indicators", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var unauthenticatedClient = _factory.CreateClient();

        // Act
        var response = await unauthenticatedClient.GetAsync("/api/indicators");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    private async Task<Collaborator> CreateNonOwnerCollaborator(Guid organizationId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Bud.Infrastructure.Persistence.ApplicationDbContext>();

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
