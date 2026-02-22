using System.Net;
using System.Net.Http.Json;
using Bud.Shared.Contracts;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bud.Server.IntegrationTests.Endpoints;

public class MetricCheckinsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _adminClient;
    private readonly CustomWebApplicationFactory _factory;

    public MetricCheckinsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _adminClient = factory.CreateGlobalAdminClient();
    }

    private async Task<Guid> GetOrCreateAdminLeader()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Bud.Server.Infrastructure.Persistence.ApplicationDbContext>();

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

        var adminLeader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Administrador",
            Email = "admin@getbud.co",
            Role = Bud.Server.Domain.Model.CollaboratorRole.Leader,
            OrganizationId = org.Id
        };
        dbContext.Collaborators.Add(adminLeader);

        var team = new Team { Id = Guid.NewGuid(), Name = "getbud.co", WorkspaceId = workspace.Id, OrganizationId = org.Id, LeaderId = adminLeader.Id };
        dbContext.Teams.Add(team);

        await dbContext.SaveChangesAsync();

        adminLeader.TeamId = team.Id;
        org.OwnerId = adminLeader.Id;
        await dbContext.SaveChangesAsync();

        return adminLeader.Id;
    }

    /// <summary>
    /// Creates an org with quantitative + qualitative metrics, plus a collaborator and tenant client.
    /// The tenant client should be used for all checkin CRUD operations (requires CollaboratorId).
    /// The admin client is used only for setup (orgs, missions, metrics).
    /// </summary>
    private async Task<(Organization Org, Metric QuantMetric, Metric QualMetric, Collaborator Collaborator, HttpClient TenantClient)> CreateTestSetup()
    {
        var leaderId = await GetOrCreateAdminLeader();
        var orgResponse = await _adminClient.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest
            {
                Name = $"checkin-org-{Guid.NewGuid():N}.com",
                OwnerId = leaderId,
            });
        var org = (await orgResponse.Content.ReadFromJsonAsync<Organization>())!;

        var missionResponse = await _adminClient.PostAsJsonAsync("/api/missions",
            new CreateMissionRequest
            {
                Name = "Checkin Test Mission",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30),
                Status = Bud.Shared.Contracts.MissionStatus.Active,
                ScopeType = Bud.Shared.Contracts.MissionScopeType.Organization,
                ScopeId = org.Id
            });
        var mission = (await missionResponse.Content.ReadFromJsonAsync<Mission>())!;

        var quantMetricResponse = await _adminClient.PostAsJsonAsync("/api/metrics",
            new CreateMetricRequest
            {
                MissionId = mission.Id,
                Name = "NPS Score",
                Type = Bud.Shared.Contracts.MetricType.Quantitative,
                QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.Achieve,
                MaxValue = 100m,
                Unit = Bud.Shared.Contracts.MetricUnit.Points
            });
        var quantMetric = (await quantMetricResponse.Content.ReadFromJsonAsync<Metric>())!;

        var qualMetricResponse = await _adminClient.PostAsJsonAsync("/api/metrics",
            new CreateMetricRequest
            {
                MissionId = mission.Id,
                Name = "Quality Standards",
                Type = Bud.Shared.Contracts.MetricType.Qualitative,
                TargetText = "Achieve high quality"
            });
        var qualMetric = (await qualMetricResponse.Content.ReadFromJsonAsync<Metric>())!;

        var collaborator = await CreateCollaboratorInOrg(org.Id);
        var tenantClient = _factory.CreateTenantClient(org.Id, collaborator.Email, collaborator.Id);

        return (org, quantMetric, qualMetric, collaborator, tenantClient);
    }

    private async Task<Collaborator> CreateCollaboratorInOrg(Guid organizationId, CollaboratorRole role = CollaboratorRole.IndividualContributor)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Bud.Server.Infrastructure.Persistence.ApplicationDbContext>();

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Colaborador Teste",
            Email = $"collab-{Guid.NewGuid():N}@test.com",
            Role = role,
            OrganizationId = organizationId
        };

        dbContext.Collaborators.Add(collaborator);
        await dbContext.SaveChangesAsync();

        return collaborator;
    }

    #region Create Tests

    [Fact]
    public async Task Create_WithQuantitativeValue_ReturnsCreated()
    {
        // Arrange
        var (_, quantMetric, _, _, client) = await CreateTestSetup();

        var request = new CreateCheckinRequest
        {
            MetricId = quantMetric.Id,
            Value = 72m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 4,
            Note = "Boa evolução no NPS"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/metrics/{request.MetricId}/checkins", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var checkin = await response.Content.ReadFromJsonAsync<MetricCheckin>();
        checkin.Should().NotBeNull();
        checkin!.Value.Should().Be(72m);
        checkin.ConfidenceLevel.Should().Be(4);
        checkin.Note.Should().Be("Boa evolução no NPS");
        checkin.MetricId.Should().Be(quantMetric.Id);
    }

    [Fact]
    public async Task Create_WithQualitativeText_ReturnsCreated()
    {
        // Arrange
        var (_, _, qualMetric, _, client) = await CreateTestSetup();

        var request = new CreateCheckinRequest
        {
            MetricId = qualMetric.Id,
            Text = "Padrões de qualidade melhoraram significativamente",
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3,
            Note = "Revisão mensal"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/metrics/{request.MetricId}/checkins", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var checkin = await response.Content.ReadFromJsonAsync<MetricCheckin>();
        checkin.Should().NotBeNull();
        checkin!.Text.Should().Be("Padrões de qualidade melhoraram significativamente");
        checkin.ConfidenceLevel.Should().Be(3);
    }

    [Fact]
    public async Task Create_WithQuantitativeMetricMissingValue_ReturnsBadRequest()
    {
        // Arrange
        var (_, quantMetric, _, _, client) = await CreateTestSetup();

        var request = new CreateCheckinRequest
        {
            MetricId = quantMetric.Id,
            // Value not set — should fail for quantitative
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/metrics/{request.MetricId}/checkins", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithQualitativeMetricMissingText_ReturnsBadRequest()
    {
        // Arrange
        var (_, _, qualMetric, _, client) = await CreateTestSetup();

        var request = new CreateCheckinRequest
        {
            MetricId = qualMetric.Id,
            // Text not set — should fail for qualitative
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/metrics/{request.MetricId}/checkins", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithInvalidConfidenceLevel_ReturnsBadRequest()
    {
        // Arrange
        var (_, quantMetric, _, _, client) = await CreateTestSetup();

        var request = new CreateCheckinRequest
        {
            MetricId = quantMetric.Id,
            Value = 50m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 6 // Invalid: must be 1-5
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/metrics/{request.MetricId}/checkins", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithNonExistentMetric_ReturnsNotFound()
    {
        // Arrange
        var (_, quantMetric, _, _, client) = await CreateTestSetup();

        var request = new CreateCheckinRequest
        {
            MetricId = Guid.NewGuid(),
            Value = 50m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/metrics/{request.MetricId}/checkins", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var unauthenticatedClient = _factory.CreateClient();

        var request = new CreateCheckinRequest
        {
            MetricId = Guid.NewGuid(),
            Value = 50m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        // Act
        var response = await unauthenticatedClient.PostAsJsonAsync($"/api/metrics/{request.MetricId}/checkins", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var (_, quantMetric, _, _, client) = await CreateTestSetup();

        var createResponse = await client.PostAsJsonAsync($"/api/metrics/{quantMetric.Id}/checkins",
            new CreateCheckinRequest
            {
                MetricId = quantMetric.Id,
                Value = 50m,
                CheckinDate = DateTime.UtcNow,
                ConfidenceLevel = 3,
                Note = "Nota original"
            });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<MetricCheckin>())!;

        var updateRequest = new PatchCheckinRequest
        {
            Value = 75m,
            CheckinDate = DateTime.UtcNow.AddDays(1),
            ConfidenceLevel = 5,
            Note = "Nota atualizada"
        };

        // Act
        var response = await client.PatchAsJsonAsync($"/api/metrics/{created.MetricId}/checkins/{created.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<MetricCheckin>();
        updated.Should().NotBeNull();
        updated!.Value.Should().Be(75m);
        updated.ConfidenceLevel.Should().Be(5);
        updated.Note.Should().Be("Nota atualizada");
    }

    [Fact]
    public async Task Update_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var (_, quantMetric, _, _, client) = await CreateTestSetup();

        var updateRequest = new PatchCheckinRequest
        {
            Value = 75m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        // Act
        var response = await client.PatchAsJsonAsync($"/api/metrics/{quantMetric.Id}/checkins/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ByDifferentCollaborator_ReturnsForbidden()
    {
        // Arrange: Create checkin as one collaborator, try to update as another
        var (org, quantMetric, _, _, client) = await CreateTestSetup();

        var createResponse = await client.PostAsJsonAsync($"/api/metrics/{quantMetric.Id}/checkins",
            new CreateCheckinRequest
            {
                MetricId = quantMetric.Id,
                Value = 50m,
                CheckinDate = DateTime.UtcNow,
                ConfidenceLevel = 3
            });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<MetricCheckin>())!;

        // Create a different collaborator and tenant client
        var otherCollaborator = await CreateCollaboratorInOrg(org.Id);
        var otherClient = _factory.CreateTenantClient(org.Id, otherCollaborator.Email, otherCollaborator.Id);

        var updateRequest = new PatchCheckinRequest
        {
            Value = 99m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 5
        };

        // Act
        var response = await otherClient.PatchAsJsonAsync($"/api/metrics/{created.MetricId}/checkins/{created.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithExistingId_ReturnsNoContent()
    {
        // Arrange
        var (_, quantMetric, _, _, client) = await CreateTestSetup();

        var createResponse = await client.PostAsJsonAsync($"/api/metrics/{quantMetric.Id}/checkins",
            new CreateCheckinRequest
            {
                MetricId = quantMetric.Id,
                Value = 50m,
                CheckinDate = DateTime.UtcNow,
                ConfidenceLevel = 3
            });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<MetricCheckin>())!;

        // Act
        var response = await client.DeleteAsync($"/api/metrics/{created.MetricId}/checkins/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's deleted
        var getResponse = await client.GetAsync($"/api/metrics/{created.MetricId}/checkins/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var (_, quantMetric, _, _, client) = await CreateTestSetup();

        // Act
        var response = await client.DeleteAsync($"/api/metrics/{quantMetric.Id}/checkins/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ByDifferentCollaborator_ReturnsForbidden()
    {
        // Arrange
        var (org, quantMetric, _, _, client) = await CreateTestSetup();

        var createResponse = await client.PostAsJsonAsync($"/api/metrics/{quantMetric.Id}/checkins",
            new CreateCheckinRequest
            {
                MetricId = quantMetric.Id,
                Value = 50m,
                CheckinDate = DateTime.UtcNow,
                ConfidenceLevel = 3
            });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<MetricCheckin>())!;

        var otherCollaborator = await CreateCollaboratorInOrg(org.Id);
        var otherClient = _factory.CreateTenantClient(org.Id, otherCollaborator.Email, otherCollaborator.Id);

        // Act
        var response = await otherClient.DeleteAsync($"/api/metrics/{created.MetricId}/checkins/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithExistingId_ReturnsOk()
    {
        // Arrange
        var (_, quantMetric, _, _, client) = await CreateTestSetup();

        var createResponse = await client.PostAsJsonAsync($"/api/metrics/{quantMetric.Id}/checkins",
            new CreateCheckinRequest
            {
                MetricId = quantMetric.Id,
                Value = 72m,
                CheckinDate = DateTime.UtcNow,
                ConfidenceLevel = 4,
                Note = "Test note"
            });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<MetricCheckin>())!;

        // Act
        var response = await client.GetAsync($"/api/metrics/{created.MetricId}/checkins/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var checkin = await response.Content.ReadFromJsonAsync<MetricCheckin>();
        checkin.Should().NotBeNull();
        checkin!.Id.Should().Be(created.Id);
        checkin.Value.Should().Be(72m);
        checkin.ConfidenceLevel.Should().Be(4);
    }

    [Fact]
    public async Task GetById_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var (_, quantMetric, _, _, client) = await CreateTestSetup();

        // Act
        var response = await client.GetAsync($"/api/metrics/{quantMetric.Id}/checkins/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WithMissionMetricIdFilter_ReturnsFilteredResults()
    {
        // Arrange
        var (_, quantMetric, qualMetric, _, client) = await CreateTestSetup();

        // Create checkins for both metrics
        await client.PostAsJsonAsync($"/api/metrics/{quantMetric.Id}/checkins",
            new CreateCheckinRequest
            {
                MetricId = quantMetric.Id,
                Value = 50m,
                CheckinDate = DateTime.UtcNow,
                ConfidenceLevel = 3
            });

        await client.PostAsJsonAsync($"/api/metrics/{qualMetric.Id}/checkins",
            new CreateCheckinRequest
            {
                MetricId = qualMetric.Id,
                Text = "Progresso qualitativo",
                CheckinDate = DateTime.UtcNow,
                ConfidenceLevel = 4
            });

        // Act — filter by quantitative metric
        var response = await client.GetAsync($"/api/metrics/{quantMetric.Id}/checkins");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<MetricCheckin>>();
        result.Should().NotBeNull();
        result!.Items.Should().OnlyContain(mc => mc.MetricId == quantMetric.Id);
    }

    [Fact]
    public async Task GetAll_WithPagination_ReturnsPagedResults()
    {
        // Arrange
        var (_, quantMetric, _, _, client) = await CreateTestSetup();

        // Create 5 checkins
        for (int i = 0; i < 5; i++)
        {
            await client.PostAsJsonAsync($"/api/metrics/{quantMetric.Id}/checkins",
                new CreateCheckinRequest
                {
                    MetricId = quantMetric.Id,
                    Value = 10m + i,
                    CheckinDate = DateTime.UtcNow.AddDays(-i),
                    ConfidenceLevel = 3
                });
        }

        // Act — page 1 with size 2
        var response = await client.GetAsync($"/api/metrics/{quantMetric.Id}/checkins?page=1&pageSize=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<MetricCheckin>>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Total.Should().BeGreaterOrEqualTo(5);
    }

    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var unauthenticatedClient = _factory.CreateClient();

        // Act
        var response = await unauthenticatedClient.GetAsync($"/api/metrics/{Guid.NewGuid()}/checkins");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Scope Authorization Tests

    [Fact]
    public async Task Create_WithTenantMismatch_ReturnsNotFound()
    {
        // Arrange: Create metric in org2, try to checkin as collaborator from org1
        // Note: Global query filters return 404 (not found) instead of 403 (forbidden)
        // This is more secure as it doesn't reveal the existence of resources in other tenants
        var leaderId = await GetOrCreateAdminLeader();

        var org1Response = await _adminClient.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest { Name = $"checkin-org1-{Guid.NewGuid():N}.com", OwnerId = leaderId });
        var org1 = (await org1Response.Content.ReadFromJsonAsync<Organization>())!;

        var org2Response = await _adminClient.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest { Name = $"checkin-org2-{Guid.NewGuid():N}.com", OwnerId = leaderId });
        var org2 = (await org2Response.Content.ReadFromJsonAsync<Organization>())!;

        // Create mission and metric in org2
        var missionResponse = await _adminClient.PostAsJsonAsync("/api/missions",
            new CreateMissionRequest
            {
                Name = "Org2 Mission",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30),
                Status = Bud.Shared.Contracts.MissionStatus.Active,
                ScopeType = Bud.Shared.Contracts.MissionScopeType.Organization,
                ScopeId = org2.Id
            });
        var mission = (await missionResponse.Content.ReadFromJsonAsync<Mission>())!;

        var metricResponse = await _adminClient.PostAsJsonAsync("/api/metrics",
            new CreateMetricRequest
            {
                MissionId = mission.Id,
                Name = "Cross-Org Metric",
                Type = Bud.Shared.Contracts.MetricType.Quantitative,
                QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.Achieve,
                MaxValue = 100m,
                Unit = Bud.Shared.Contracts.MetricUnit.Points
            });
        var metric = (await metricResponse.Content.ReadFromJsonAsync<Metric>())!;

        // Create collaborator in org1 and try to checkin on org2's metric
        var collaborator = await CreateCollaboratorInOrg(org1.Id);
        var tenantClient = _factory.CreateTenantClient(org1.Id, collaborator.Email, collaborator.Id);

        var request = new CreateCheckinRequest
        {
            MetricId = metric.Id,
            Value = 50m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        // Act
        var response = await tenantClient.PostAsJsonAsync($"/api/metrics/{request.MetricId}/checkins", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithCollaboratorScopedMission_OnlyAuthorCanCheckin()
    {
        // Arrange: Create collaborator-scoped mission
        var leaderId = await GetOrCreateAdminLeader();

        var orgResponse = await _adminClient.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest { Name = $"scope-collab-{Guid.NewGuid():N}.com", OwnerId = leaderId });
        var org = (await orgResponse.Content.ReadFromJsonAsync<Organization>())!;

        var owner = await CreateCollaboratorInOrg(org.Id);
        var other = await CreateCollaboratorInOrg(org.Id);

        // Create collaborator-scoped mission
        var missionResponse = await _adminClient.PostAsJsonAsync("/api/missions",
            new CreateMissionRequest
            {
                Name = "Personal Mission",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30),
                Status = Bud.Shared.Contracts.MissionStatus.Active,
                ScopeType = Bud.Shared.Contracts.MissionScopeType.Collaborator,
                ScopeId = owner.Id
            });
        var mission = (await missionResponse.Content.ReadFromJsonAsync<Mission>())!;

        var metricResponse = await _adminClient.PostAsJsonAsync("/api/metrics",
            new CreateMetricRequest
            {
                MissionId = mission.Id,
                Name = "Personal Metric",
                Type = Bud.Shared.Contracts.MetricType.Quantitative,
                QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.Achieve,
                MaxValue = 100m,
                Unit = Bud.Shared.Contracts.MetricUnit.Points
            });
        var metric = (await metricResponse.Content.ReadFromJsonAsync<Metric>())!;

        var request = new CreateCheckinRequest
        {
            MetricId = metric.Id,
            Value = 50m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        // Act: Owner should succeed
        var ownerClient = _factory.CreateTenantClient(org.Id, owner.Email, owner.Id);
        var ownerResponse = await ownerClient.PostAsJsonAsync($"/api/metrics/{request.MetricId}/checkins", request);
        ownerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act: Other collaborator should fail
        var otherClient = _factory.CreateTenantClient(org.Id, other.Email, other.Id);
        var otherResponse = await otherClient.PostAsJsonAsync($"/api/metrics/{request.MetricId}/checkins", request);
        otherResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_WithTeamScopedMission_TeamMemberCanCheckin()
    {
        // Arrange: Create team-scoped mission
        var adminLeaderId = await GetOrCreateAdminLeader();

        var orgResponse = await _adminClient.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest { Name = $"scope-team-{Guid.NewGuid():N}.com", OwnerId = adminLeaderId });
        var org = (await orgResponse.Content.ReadFromJsonAsync<Organization>())!;

        var wsResponse = await _adminClient.PostAsJsonAsync("/api/workspaces",
            new CreateWorkspaceRequest { Name = "Test WS", OrganizationId = org.Id });
        var ws = (await wsResponse.Content.ReadFromJsonAsync<Workspace>())!;

        // Create leader in the new org for team creation
        var orgLeader = await CreateCollaboratorInOrg(org.Id, CollaboratorRole.Leader);

        var teamResponse = await _adminClient.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = "Test Team", WorkspaceId = ws.Id, LeaderId = orgLeader.Id });
        var team = (await teamResponse.Content.ReadFromJsonAsync<Team>())!;

        // Create collaborators
        var member = await CreateCollaboratorInOrg(org.Id);
        var outsider = await CreateCollaboratorInOrg(org.Id);

        // Add member to team
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<Bud.Server.Infrastructure.Persistence.ApplicationDbContext>();
            dbContext.Set<CollaboratorTeam>().Add(new CollaboratorTeam
            {
                CollaboratorId = member.Id,
                TeamId = team.Id
            });
            await dbContext.SaveChangesAsync();
        }

        // Create team-scoped mission with metric
        var missionResponse = await _adminClient.PostAsJsonAsync("/api/missions",
            new CreateMissionRequest
            {
                Name = "Team Mission",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30),
                Status = Bud.Shared.Contracts.MissionStatus.Active,
                ScopeType = Bud.Shared.Contracts.MissionScopeType.Team,
                ScopeId = team.Id
            });
        var mission = (await missionResponse.Content.ReadFromJsonAsync<Mission>())!;

        var metricResponse = await _adminClient.PostAsJsonAsync("/api/metrics",
            new CreateMetricRequest
            {
                MissionId = mission.Id,
                Name = "Team Metric",
                Type = Bud.Shared.Contracts.MetricType.Quantitative,
                QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.Achieve,
                MaxValue = 100m,
                Unit = Bud.Shared.Contracts.MetricUnit.Points
            });
        var metric = (await metricResponse.Content.ReadFromJsonAsync<Metric>())!;

        var request = new CreateCheckinRequest
        {
            MetricId = metric.Id,
            Value = 50m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        // Act: Team member should succeed
        var memberClient = _factory.CreateTenantClient(org.Id, member.Email, member.Id);
        var memberResponse = await memberClient.PostAsJsonAsync($"/api/metrics/{request.MetricId}/checkins", request);
        memberResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act: Outsider should fail
        var outsiderClient = _factory.CreateTenantClient(org.Id, outsider.Email, outsider.Id);
        var outsiderResponse = await outsiderClient.PostAsJsonAsync($"/api/metrics/{request.MetricId}/checkins", request);
        outsiderResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion
}
