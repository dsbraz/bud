using System.Net;
using System.Net.Http.Json;
using System.Diagnostics;
using Bud.Shared.Contracts;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
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

        // Create collaborator first (without team) so we can reference it as team leader
        var adminLeader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Administrador",
            Email = "admin@getbud.co",
            Role = Bud.Server.Domain.Model.CollaboratorRole.Leader,
            TeamId = null,
            OrganizationId = org.Id
        };
        dbContext.Collaborators.Add(adminLeader);

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "getbud.co",
            WorkspaceId = workspace.Id,
            OrganizationId = org.Id,
            LeaderId = adminLeader.Id
        };
        dbContext.Teams.Add(team);

        await dbContext.SaveChangesAsync();

        // Now update collaborator's team and organization owner
        adminLeader.TeamId = team.Id;
        org.OwnerId = adminLeader.Id;
        await dbContext.SaveChangesAsync();

        return adminLeader.Id;
    }

    private async Task<(Organization org, Workspace workspace, Guid leaderId)> CreateTestHierarchy()
    {
        var adminLeaderId = await GetOrCreateAdminLeader();
        var orgResponse = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest
            {
                Name = "test-org.com",
                OwnerId = adminLeaderId,
            });
        var org = await orgResponse.Content.ReadFromJsonAsync<Organization>();

        var workspaceResponse = await _client.PostAsJsonAsync("/api/workspaces",
            new CreateWorkspaceRequest { Name = "Test Workspace", OrganizationId = org!.Id });
        var workspace = await workspaceResponse.Content.ReadFromJsonAsync<Workspace>();

        // Create a Leader collaborator in the new org (required for team creation)
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Bud.Server.Infrastructure.Persistence.ApplicationDbContext>();
        var leader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Líder Teste",
            Email = $"leader-{Guid.NewGuid():N}@test-org.com",
            Role = Bud.Server.Domain.Model.CollaboratorRole.Leader,
            OrganizationId = org!.Id,
            TeamId = null
        };
        dbContext.Collaborators.Add(leader);
        await dbContext.SaveChangesAsync();

        return (org!, workspace!, leader.Id);
    }

    private async Task<Collaborator> CreateNonOwnerCollaborator(Guid organizationId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Bud.Server.Infrastructure.Persistence.ApplicationDbContext>();

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Colaborador Teste",
            Email = $"colaborador-{Guid.NewGuid():N}@example.com",
            Role = Bud.Server.Domain.Model.CollaboratorRole.IndividualContributor,
            OrganizationId = organizationId,
            TeamId = null
        };

        dbContext.Collaborators.Add(collaborator);
        await dbContext.SaveChangesAsync();

        return collaborator;
    }

    #region Create Tests

    [Fact]
    public async Task GetAll_WithInvalidPageSize_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/teams?page=1&pageSize=101");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Detail.Should().Be("O parâmetro 'pageSize' deve estar entre 1 e 100.");
    }

    [Fact]
    public async Task GetAll_ShouldRespondWithinBudget()
    {
        var watch = Stopwatch.StartNew();

        var response = await _client.GetAsync("/api/teams?page=1&pageSize=20");

        watch.Stop();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        watch.ElapsedMilliseconds.Should().BeLessThan(5000);
    }

    [Fact]
    public async Task Create_WithValidParentTeam_ReturnsCreated()
    {
        // Arrange
        var (_, workspace, leaderId) = await CreateTestHierarchy();

        // Create parent team
        var parentResponse = await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = "Parent Team", WorkspaceId = workspace.Id, LeaderId = leaderId });
        var parentTeam = await parentResponse.Content.ReadFromJsonAsync<Team>();

        // Create child team
        var request = new CreateTeamRequest
        {
            Name = "Child Team",
            WorkspaceId = workspace.Id,
            LeaderId = leaderId,
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
    public async Task Create_AsNonOwner_ReturnsForbidden()
    {
        // Arrange
        var (org, workspace, leaderId) = await CreateTestHierarchy();
        var collaborator = await CreateNonOwnerCollaborator(org.Id);
        var tenantClient = _factory.CreateTenantClient(org.Id, collaborator.Email, collaborator.Id);

        var request = new CreateTeamRequest
        {
            Name = "Team NonOwner",
            WorkspaceId = workspace.Id,
            LeaderId = leaderId
        };

        // Act
        var response = await tenantClient.PostAsJsonAsync("/api/teams", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_WithParentInDifferentWorkspace_ReturnsBadRequest()
    {
        // Arrange: Create two workspaces
        var (org, _, leaderId) = await CreateTestHierarchy();

        var workspace1Response = await _client.PostAsJsonAsync("/api/workspaces",
            new CreateWorkspaceRequest { Name = "Workspace 1", OrganizationId = org.Id });
        var workspace1 = (await workspace1Response.Content.ReadFromJsonAsync<Workspace>())!;

        var workspace2Response = await _client.PostAsJsonAsync("/api/workspaces",
            new CreateWorkspaceRequest { Name = "Workspace 2", OrganizationId = org.Id });
        var workspace2 = (await workspace2Response.Content.ReadFromJsonAsync<Workspace>())!;

        // Create parent team in workspace1
        var parentResponse = await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = "Parent Team", WorkspaceId = workspace1.Id, LeaderId = leaderId });
        var parentTeam = await parentResponse.Content.ReadFromJsonAsync<Team>();

        // Try to create child team in workspace2 with parent from workspace1
        var request = new CreateTeamRequest
        {
            Name = "Child Team",
            WorkspaceId = workspace2.Id,
            LeaderId = leaderId,
            ParentTeamId = parentTeam!.Id
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/teams", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Tenant Isolation Tests

    [Fact]
    public async Task GetAll_WithTenantClient_ShouldNotReturnTeamsFromAnotherTenant()
    {
        // Arrange
        var (orgA, workspaceA, leaderIdA) = await CreateTestHierarchy();
        var (orgB, workspaceB, leaderIdB) = await CreateTestHierarchy();

        var teamAName = $"Isolated-A-{Guid.NewGuid():N}";
        var teamBName = $"Isolated-B-{Guid.NewGuid():N}";

        var teamAResponse = await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = teamAName, WorkspaceId = workspaceA.Id, LeaderId = leaderIdA });
        teamAResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var teamBResponse = await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = teamBName, WorkspaceId = workspaceB.Id, LeaderId = leaderIdB });
        teamBResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var collaboratorA = await CreateNonOwnerCollaborator(orgA.Id);
        var tenantClientA = _factory.CreateTenantClient(orgA.Id, collaboratorA.Email, collaboratorA.Id);

        // Act
        var visibleResponse = await tenantClientA.GetAsync($"/api/teams?search={teamAName}");
        var hiddenResponse = await tenantClientA.GetAsync($"/api/teams?search={teamBName}");

        // Assert
        visibleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        hiddenResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var visiblePayload = await visibleResponse.Content.ReadFromJsonAsync<PagedResult<Team>>();
        var hiddenPayload = await hiddenResponse.Content.ReadFromJsonAsync<PagedResult<Team>>();

        visiblePayload.Should().NotBeNull();
        hiddenPayload.Should().NotBeNull();
        visiblePayload!.Items.Should().ContainSingle(t => t.Name == teamAName);
        hiddenPayload!.Items.Should().BeEmpty();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Patch_SettingSelfAsParent_ReturnsBadRequest()
    {
        // Arrange
        var (_, workspace, leaderId) = await CreateTestHierarchy();

        var createResponse = await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = "Test Team", WorkspaceId = workspace.Id, LeaderId = leaderId });
        var team = await createResponse.Content.ReadFromJsonAsync<Team>();

        // Try to set itself as parent
        var updateRequest = new PatchTeamRequest
        {
            Name = "Test Team",
            LeaderId = leaderId,
            ParentTeamId = team!.Id
        };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/teams/{team.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_WithValidParent_ReturnsOk()
    {
        // Arrange
        var (_, workspace, leaderId) = await CreateTestHierarchy();

        // Create two teams
        var team1Response = await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = "Team 1", WorkspaceId = workspace.Id, LeaderId = leaderId });
        var team1 = await team1Response.Content.ReadFromJsonAsync<Team>();

        var team2Response = await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = "Team 2", WorkspaceId = workspace.Id, LeaderId = leaderId });
        var team2 = await team2Response.Content.ReadFromJsonAsync<Team>();

        // Update team2 to have team1 as parent
        var updateRequest = new PatchTeamRequest
        {
            Name = "Team 2 Updated",
            LeaderId = leaderId,
            ParentTeamId = team1!.Id
        };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/teams/{team2!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<Team>();
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Team 2 Updated");
        updated.ParentTeamId.Should().Be(team1.Id);
    }

    [Fact]
    public async Task Update_WhenNotOwner_ReturnsForbidden()
    {
        // Arrange
        var (org, workspace, leaderId) = await CreateTestHierarchy();

        var createResponse = await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = "Team A", WorkspaceId = workspace.Id, LeaderId = leaderId });
        var team = await createResponse.Content.ReadFromJsonAsync<Team>();

        var collaborator = await CreateNonOwnerCollaborator(org.Id);
        var tenantClient = _factory.CreateTenantClient(org.Id, collaborator.Email, collaborator.Id);

        var updateRequest = new PatchTeamRequest
        {
            Name = "Team A Updated",
            LeaderId = leaderId
        };

        // Act
        var response = await tenantClient.PatchAsJsonAsync($"/api/teams/{team!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithAssociatedMissions_ReturnsConflict()
    {
        // Arrange
        var (org, workspace, leaderId) = await CreateTestHierarchy();

        var teamResponse = await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = "Team with Mission", WorkspaceId = workspace.Id, LeaderId = leaderId });
        var team = (await teamResponse.Content.ReadFromJsonAsync<Team>())!;

        // Create mission scoped to team via DbContext
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Bud.Server.Infrastructure.Persistence.ApplicationDbContext>();
        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Missão Team",
            OrganizationId = org.Id,
            TeamId = team.Id,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = Bud.Server.Domain.Model.MissionStatus.Active
        };
        dbContext.Missions.Add(mission);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/teams/{team.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_WithSubTeams_ReturnsConflict()
    {
        // Arrange
        var (_, workspace, leaderId) = await CreateTestHierarchy();

        // Create parent team
        var parentResponse = await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = "Parent Team", WorkspaceId = workspace.Id, LeaderId = leaderId });
        var parentTeam = await parentResponse.Content.ReadFromJsonAsync<Team>();

        // Create sub-team
        await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest
            {
                Name = "Sub Team",
                WorkspaceId = workspace.Id,
                LeaderId = leaderId,
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
        var (_, workspace, leaderId) = await CreateTestHierarchy();

        var createResponse = await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = "Team to Delete", WorkspaceId = workspace.Id, LeaderId = leaderId });
        var team = await createResponse.Content.ReadFromJsonAsync<Team>();

        // Act
        var response = await _client.DeleteAsync($"/api/teams/{team!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's deleted
        var getResponse = await _client.GetAsync($"/api/teams/{team.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WhenNotOwner_ReturnsForbidden()
    {
        // Arrange
        var (org, workspace, leaderId) = await CreateTestHierarchy();

        var createResponse = await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = "Team B", WorkspaceId = workspace.Id, LeaderId = leaderId });
        var team = await createResponse.Content.ReadFromJsonAsync<Team>();

        var collaborator = await CreateNonOwnerCollaborator(org.Id);
        var tenantClient = _factory.CreateTenantClient(org.Id, collaborator.Email, collaborator.Id);

        // Act
        var response = await tenantClient.DeleteAsync($"/api/teams/{team!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GetSubTeams Tests

    [Fact]
    public async Task GetSubTeams_ReturnsSubTeamsOnly()
    {
        // Arrange
        var (_, workspace, leaderId) = await CreateTestHierarchy();

        // Create parent team
        var parentResponse = await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = "Parent Team", WorkspaceId = workspace.Id, LeaderId = leaderId });
        var parentTeam = await parentResponse.Content.ReadFromJsonAsync<Team>();

        // Create sub-teams
        await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest
            {
                Name = "Sub Team 1",
                WorkspaceId = workspace.Id,
                LeaderId = leaderId,
                ParentTeamId = parentTeam!.Id
            });

        await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest
            {
                Name = "Sub Team 2",
                WorkspaceId = workspace.Id,
                LeaderId = leaderId,
                ParentTeamId = parentTeam.Id
            });

        // Create unrelated team
        await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = "Unrelated Team", WorkspaceId = workspace.Id, LeaderId = leaderId });

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

    #region Authorization Tests

    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var unauthenticatedClient = _factory.CreateClient();

        // Act
        var response = await unauthenticatedClient.GetAsync("/api/teams");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GetCollaborators Tests

    [Fact]
    public async Task GetCollaborators_ReturnsTeamCollaborators()
    {
        // Arrange
        var (org, workspace, leaderId) = await CreateTestHierarchy();

        var teamResponse = await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = "Test Team", WorkspaceId = workspace.Id, LeaderId = leaderId });
        var team = await teamResponse.Content.ReadFromJsonAsync<Team>();

        // Create collaborators directly in database (since API doesn't support creating with TeamId)
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Bud.Server.Infrastructure.Persistence.ApplicationDbContext>();

        var collab1 = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Collaborator 1",
            Email = "collab1@example.com",
            Role = Bud.Server.Domain.Model.CollaboratorRole.IndividualContributor,
            OrganizationId = org.Id,
            TeamId = team!.Id
        };

        var collab2 = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Collaborator 2",
            Email = "collab2@example.com",
            Role = Bud.Server.Domain.Model.CollaboratorRole.Leader,
            OrganizationId = org.Id,
            TeamId = team.Id
        };

        dbContext.Collaborators.AddRange(collab1, collab2);
        await dbContext.SaveChangesAsync();

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

    #region Leader CollaboratorTeam Sync Tests

    [Fact]
    public async Task Create_ShouldIncludeLeaderInCollaboratorSummaries()
    {
        // Arrange
        var (_, workspace, leaderId) = await CreateTestHierarchy();

        // Act: create team
        var createResponse = await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = "Leader Sync Team", WorkspaceId = workspace.Id, LeaderId = leaderId });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var team = await createResponse.Content.ReadFromJsonAsync<Team>();

        // Assert: leader should be in collaborators summary projection
        var summariesResponse = await _client.GetAsync($"/api/teams/{team!.Id}/collaborators/lookup");
        summariesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var summaries = await summariesResponse.Content.ReadFromJsonAsync<List<CollaboratorSummaryDto>>();
        summaries.Should().NotBeNull();
        summaries.Should().Contain(c => c.Id == leaderId);
    }

    [Fact]
    public async Task PatchCollaborators_WithoutLeader_ShouldReturnBadRequest()
    {
        // Arrange
        var (org, workspace, leaderId) = await CreateTestHierarchy();

        var createResponse = await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = "Leader Protected Team", WorkspaceId = workspace.Id, LeaderId = leaderId });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var team = await createResponse.Content.ReadFromJsonAsync<Team>();

        // Create another collaborator to use in the update
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Bud.Server.Infrastructure.Persistence.ApplicationDbContext>();
        var otherCollab = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Other Collaborator",
            Email = $"other-{Guid.NewGuid():N}@test-org.com",
            Role = Bud.Server.Domain.Model.CollaboratorRole.IndividualContributor,
            OrganizationId = org.Id
        };
        dbContext.Collaborators.Add(otherCollab);
        await dbContext.SaveChangesAsync();

        // Act: update collaborators WITHOUT the leader
        var updateResponse = await _client.PatchAsJsonAsync($"/api/teams/{team!.Id}/collaborators",
            new PatchTeamCollaboratorsRequest { CollaboratorIds = new List<Guid> { otherCollab.Id } });

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
