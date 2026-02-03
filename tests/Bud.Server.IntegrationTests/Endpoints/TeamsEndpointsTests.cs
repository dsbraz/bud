using System.Net;
using System.Net.Http.Json;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bud.Server.IntegrationTests.Endpoints;

public class TeamsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public TeamsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateGlobalAdminClient();
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

    private async Task<(Organization org, Workspace workspace)> CreateTestHierarchy()
    {
        var leaderId = await GetOrCreateAdminLeader();
        var orgResponse = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest
            {
                Name = "test-org.com",
                OwnerId = leaderId,
                UserEmail = "admin@getbud.co"
            });
        var org = await orgResponse.Content.ReadFromJsonAsync<Organization>();

        var workspaceResponse = await _client.PostAsJsonAsync("/api/workspaces",
            new CreateWorkspaceRequest { Name = "Test Workspace", OrganizationId = org!.Id, Visibility = Visibility.Public });
        var workspace = await workspaceResponse.Content.ReadFromJsonAsync<Workspace>();

        return (org!, workspace!);
    }

    #region Create Tests

    [Fact]
    public async Task Create_WithValidParentTeam_ReturnsCreated()
    {
        // Arrange
        var (_, workspace) = await CreateTestHierarchy();

        // Create parent team
        var parentResponse = await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = "Parent Team", WorkspaceId = workspace.Id });
        var parentTeam = await parentResponse.Content.ReadFromJsonAsync<Team>();

        // Create child team
        var request = new CreateTeamRequest
        {
            Name = "Child Team",
            WorkspaceId = workspace.Id,
            ParentTeamId = parentTeam!.Id
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/teams", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var team = await response.Content.ReadFromJsonAsync<Team>();
        team.Should().NotBeNull();
        team!.Name.Should().Be("Child Team");
        team.ParentTeamId.Should().Be(parentTeam.Id);
    }

    [Fact]
    public async Task Create_WithParentInDifferentWorkspace_ReturnsBadRequest()
    {
        // Arrange: Create two workspaces
        var leaderId = await GetOrCreateAdminLeader();
        var org = (await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest
            {
                Name = "test-org.com",
                OwnerId = leaderId,
                UserEmail = "admin@getbud.co"
            })
        ).Content.ReadFromJsonAsync<Organization>().Result!;

        var workspace1 = (await _client.PostAsJsonAsync("/api/workspaces",
            new CreateWorkspaceRequest { Name = "Workspace 1", OrganizationId = org.Id, Visibility = Visibility.Public })
        ).Content.ReadFromJsonAsync<Workspace>().Result!;

        var workspace2 = (await _client.PostAsJsonAsync("/api/workspaces",
            new CreateWorkspaceRequest { Name = "Workspace 2", OrganizationId = org.Id, Visibility = Visibility.Public })
        ).Content.ReadFromJsonAsync<Workspace>().Result!;

        // Create parent team in workspace1
        var parentResponse = await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = "Parent Team", WorkspaceId = workspace1.Id });
        var parentTeam = await parentResponse.Content.ReadFromJsonAsync<Team>();

        // Try to create child team in workspace2 with parent from workspace1
        var request = new CreateTeamRequest
        {
            Name = "Child Team",
            WorkspaceId = workspace2.Id,
            ParentTeamId = parentTeam!.Id
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/teams", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_SettingSelfAsParent_ReturnsBadRequest()
    {
        // Arrange
        var (_, workspace) = await CreateTestHierarchy();

        var createResponse = await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = "Test Team", WorkspaceId = workspace.Id });
        var team = await createResponse.Content.ReadFromJsonAsync<Team>();

        // Try to set itself as parent
        var updateRequest = new UpdateTeamRequest
        {
            Name = "Test Team",
            ParentTeamId = team!.Id
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/teams/{team.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_WithValidParent_ReturnsOk()
    {
        // Arrange
        var (_, workspace) = await CreateTestHierarchy();

        // Create two teams
        var team1Response = await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = "Team 1", WorkspaceId = workspace.Id });
        var team1 = await team1Response.Content.ReadFromJsonAsync<Team>();

        var team2Response = await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = "Team 2", WorkspaceId = workspace.Id });
        var team2 = await team2Response.Content.ReadFromJsonAsync<Team>();

        // Update team2 to have team1 as parent
        var updateRequest = new UpdateTeamRequest
        {
            Name = "Team 2 Updated",
            ParentTeamId = team1!.Id
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/teams/{team2!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<Team>();
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Team 2 Updated");
        updated.ParentTeamId.Should().Be(team1.Id);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithSubTeams_ReturnsConflict()
    {
        // Arrange
        var (_, workspace) = await CreateTestHierarchy();

        // Create parent team
        var parentResponse = await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = "Parent Team", WorkspaceId = workspace.Id });
        var parentTeam = await parentResponse.Content.ReadFromJsonAsync<Team>();

        // Create sub-team
        await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest
            {
                Name = "Sub Team",
                WorkspaceId = workspace.Id,
                ParentTeamId = parentTeam!.Id
            });

        // Act - Try to delete parent team
        var response = await _client.DeleteAsync($"/api/teams/{parentTeam.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_WithoutSubTeams_ReturnsNoContent()
    {
        // Arrange
        var (_, workspace) = await CreateTestHierarchy();

        var createResponse = await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = "Team to Delete", WorkspaceId = workspace.Id });
        var team = await createResponse.Content.ReadFromJsonAsync<Team>();

        // Act
        var response = await _client.DeleteAsync($"/api/teams/{team!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's deleted
        var getResponse = await _client.GetAsync($"/api/teams/{team.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetSubTeams Tests

    [Fact]
    public async Task GetSubTeams_ReturnsSubTeamsOnly()
    {
        // Arrange
        var (_, workspace) = await CreateTestHierarchy();

        // Create parent team
        var parentResponse = await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = "Parent Team", WorkspaceId = workspace.Id });
        var parentTeam = await parentResponse.Content.ReadFromJsonAsync<Team>();

        // Create sub-teams
        await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest
            {
                Name = "Sub Team 1",
                WorkspaceId = workspace.Id,
                ParentTeamId = parentTeam!.Id
            });

        await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest
            {
                Name = "Sub Team 2",
                WorkspaceId = workspace.Id,
                ParentTeamId = parentTeam.Id
            });

        // Create unrelated team
        await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = "Unrelated Team", WorkspaceId = workspace.Id });

        // Act
        var response = await _client.GetAsync($"/api/teams/{parentTeam.Id}/subteams");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<Team>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(t => t.ParentTeamId == parentTeam.Id);
    }

    #endregion

    #region GetCollaborators Tests

    [Fact]
    public async Task GetCollaborators_ReturnsTeamCollaborators()
    {
        // Arrange
        var (_, workspace) = await CreateTestHierarchy();

        var teamResponse = await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = "Test Team", WorkspaceId = workspace.Id });
        var team = await teamResponse.Content.ReadFromJsonAsync<Team>();

        // Create collaborators
        await _client.PostAsJsonAsync("/api/collaborators",
            new CreateCollaboratorRequest
            {
                FullName = "Collaborator 1",
                Email = "collab1@example.com",
                Role = CollaboratorRole.IndividualContributor,
                TeamId = team!.Id
            });

        await _client.PostAsJsonAsync("/api/collaborators",
            new CreateCollaboratorRequest
            {
                FullName = "Collaborator 2",
                Email = "collab2@example.com",
                Role = CollaboratorRole.Leader,
                TeamId = team.Id
            });

        // Act
        var response = await _client.GetAsync($"/api/teams/{team.Id}/collaborators");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<Collaborator>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(c => c.TeamId == team.Id);
    }

    #endregion
}
