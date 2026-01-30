using System.Net;
using System.Net.Http.Json;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bud.Server.IntegrationTests.Endpoints;

public class MissionsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public MissionsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAdminClient();
    }

    private async Task<Guid> GetOrCreateAdminLeader()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Bud.Server.Data.ApplicationDbContext>();

        var existingLeader = await dbContext.Collaborators
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

    #region Create Tests

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreated()
    {
        // Arrange: Create organization first
        var leaderId = await GetOrCreateAdminLeader();
        var orgResponse = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest
            {
                Name = "test-mission.com",
                OwnerId = leaderId,
                UserEmail = "admin@getbud.co"
            });
        var org = await orgResponse.Content.ReadFromJsonAsync<Organization>();

        var request = new CreateMissionRequest
        {
            Name = "Test Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Organization,
            ScopeId = org!.Id
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/missions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var mission = await response.Content.ReadFromJsonAsync<Mission>();
        mission.Should().NotBeNull();
        mission!.Name.Should().Be("Test Mission");
        mission.OrganizationId.Should().Be(org.Id);
        mission.WorkspaceId.Should().BeNull();
        mission.TeamId.Should().BeNull();
        mission.CollaboratorId.Should().BeNull();
    }

    [Fact]
    public async Task Create_WithInvalidScopeId_ReturnsNotFound()
    {
        // Arrange
        var request = new CreateMissionRequest
        {
            Name = "Test Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Organization,
            ScopeId = Guid.NewGuid() // Non-existent ID
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/missions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithEndDateBeforeStartDate_ReturnsBadRequest()
    {
        // Arrange: Create organization first
        var leaderId = await GetOrCreateAdminLeader();
        var orgResponse = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest
            {
                Name = "test-org.com",
                OwnerId = leaderId,
                UserEmail = "admin@getbud.co"
            });
        var org = await orgResponse.Content.ReadFromJsonAsync<Organization>();

        var request = new CreateMissionRequest
        {
            Name = "Test Mission",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow, // Before start date
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Organization,
            ScopeId = org!.Id
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/missions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithExistingId_ReturnsOk()
    {
        // Arrange: Create organization and mission
        var leaderId = await GetOrCreateAdminLeader();
        var orgResponse = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest { Name = "test-org.com", OwnerId = leaderId, UserEmail = "admin@getbud.co" });
        var org = await orgResponse.Content.ReadFromJsonAsync<Organization>();

        var createRequest = new CreateMissionRequest
        {
            Name = "Test Mission for GetById",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Organization,
            ScopeId = org!.Id
        };
        var createResponse = await _client.PostAsJsonAsync("/api/missions", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<Mission>();

        // Act
        var response = await _client.GetAsync($"/api/missions/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var mission = await response.Content.ReadFromJsonAsync<Mission>();
        mission.Should().NotBeNull();
        mission!.Id.Should().Be(created.Id);
        mission.Name.Should().Be("Test Mission for GetById");
    }

    [Fact]
    public async Task GetById_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/missions/{nonExistingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WithScopeFilter_ReturnsFilteredResults()
    {
        // Arrange: Create two organizations
        var leaderId = await GetOrCreateAdminLeader();
        var org1Response = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest
            {
                Name = "org1.com",
                OwnerId = leaderId,
                UserEmail = "admin@getbud.co"
            });
        var org1 = await org1Response.Content.ReadFromJsonAsync<Organization>();

        var org2Response = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest
            {
                Name = "org2.com",
                OwnerId = leaderId,
                UserEmail = "admin@getbud.co"
            });
        var org2 = await org2Response.Content.ReadFromJsonAsync<Organization>();

        // Create missions for each org
        await _client.PostAsJsonAsync("/api/missions", new CreateMissionRequest
        {
            Name = "Mission Org 1",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Organization,
            ScopeId = org1!.Id
        });

        await _client.PostAsJsonAsync("/api/missions", new CreateMissionRequest
        {
            Name = "Mission Org 2",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Organization,
            ScopeId = org2!.Id
        });

        // Act - Filter by org1
        var response = await _client.GetAsync($"/api/missions?scopeType={MissionScopeType.Organization}&scopeId={org1.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<Mission>>();
        result.Should().NotBeNull();
        result!.Items.Should().OnlyContain(m => m.OrganizationId == org1.Id);
    }

    [Fact]
    public async Task GetAll_WithPagination_ReturnsPagedResults()
    {
        // Arrange: Create organization and multiple missions
        var leaderId = await GetOrCreateAdminLeader();
        var orgResponse = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest { Name = "test-org.com", OwnerId = leaderId, UserEmail = "admin@getbud.co" });
        var org = await orgResponse.Content.ReadFromJsonAsync<Organization>();

        for (int i = 1; i <= 15; i++)
        {
            await _client.PostAsJsonAsync("/api/missions", new CreateMissionRequest
            {
                Name = $"Mission {i}",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(7),
                Status = MissionStatus.Planned,
                ScopeType = MissionScopeType.Organization,
                ScopeId = org!.Id
            });
        }

        // Act
        var response = await _client.GetAsync("/api/missions?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<Mission>>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.Items.Should().HaveCountLessOrEqualTo(10);
    }

    #endregion

    #region GetMyMissions Tests

    [Fact]
    public async Task GetMyMissions_WithValidCollaborator_ReturnsHierarchyMissions()
    {
        // Arrange: Create full hierarchy
        var leaderId = await GetOrCreateAdminLeader();
        var orgResponse = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest { Name = "test-org.com", OwnerId = leaderId, UserEmail = "admin@getbud.co" });
        var org = await orgResponse.Content.ReadFromJsonAsync<Organization>();

        var workspaceResponse = await _client.PostAsJsonAsync("/api/workspaces",
            new CreateWorkspaceRequest { Name = "Test Workspace", OrganizationId = org!.Id });
        var workspace = await workspaceResponse.Content.ReadFromJsonAsync<Workspace>();

        var teamResponse = await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = "Test Team", WorkspaceId = workspace!.Id });
        var team = await teamResponse.Content.ReadFromJsonAsync<Team>();

        var collaboratorResponse = await _client.PostAsJsonAsync("/api/collaborators",
            new CreateCollaboratorRequest
            {
                FullName = "Test User",
                Email = "test@example.com",
                Role = CollaboratorRole.IndividualContributor,
                TeamId = team!.Id
            });
        var collaborator = await collaboratorResponse.Content.ReadFromJsonAsync<Collaborator>();

        // Create missions at each level
        await _client.PostAsJsonAsync("/api/missions", new CreateMissionRequest
        {
            Name = "Org Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Organization,
            ScopeId = org.Id
        });

        await _client.PostAsJsonAsync("/api/missions", new CreateMissionRequest
        {
            Name = "Collaborator Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Collaborator,
            ScopeId = collaborator!.Id
        });

        // Act
        var response = await _client.GetAsync($"/api/missions/my-missions/{collaborator.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<Mission>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCountGreaterOrEqualTo(2);
        result.Items.Should().Contain(m => m.Name == "Org Mission");
        result.Items.Should().Contain(m => m.Name == "Collaborator Mission");
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidRequest_ReturnsOk()
    {
        // Arrange: Create organization and mission
        var leaderId = await GetOrCreateAdminLeader();
        var orgResponse = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest { Name = "test-org.com", OwnerId = leaderId, UserEmail = "admin@getbud.co" });
        var org = await orgResponse.Content.ReadFromJsonAsync<Organization>();

        var createRequest = new CreateMissionRequest
        {
            Name = "Original Name",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Organization,
            ScopeId = org!.Id
        };
        var createResponse = await _client.PostAsJsonAsync("/api/missions", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<Mission>();

        var updateRequest = new UpdateMissionRequest
        {
            Name = "Updated Name",
            StartDate = created!.StartDate,
            EndDate = created.EndDate,
            Status = MissionStatus.Active
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/missions/{created.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<Mission>();
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Name");
        updated.Status.Should().Be(MissionStatus.Active);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithExistingId_ReturnsNoContent()
    {
        // Arrange: Create organization and mission
        var leaderId = await GetOrCreateAdminLeader();
        var orgResponse = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest { Name = "test-org.com", OwnerId = leaderId, UserEmail = "admin@getbud.co" });
        var org = await orgResponse.Content.ReadFromJsonAsync<Organization>();

        var createRequest = new CreateMissionRequest
        {
            Name = "Mission to Delete",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Organization,
            ScopeId = org!.Id
        };
        var createResponse = await _client.PostAsJsonAsync("/api/missions", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<Mission>();

        // Act
        var response = await _client.DeleteAsync($"/api/missions/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's deleted
        var getResponse = await _client.GetAsync($"/api/missions/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
