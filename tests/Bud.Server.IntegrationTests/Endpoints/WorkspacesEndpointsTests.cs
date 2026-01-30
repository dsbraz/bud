using System.Net;
using System.Net.Http.Json;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bud.Server.IntegrationTests.Endpoints;

public class WorkspacesEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _adminClient;
    private readonly CustomWebApplicationFactory _factory;

    public WorkspacesEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _adminClient = factory.CreateAdminClient();
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

        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "getbud.co",
            Visibility = Visibility.Public,
            OrganizationId = org.Id
        };
        dbContext.Workspaces.Add(workspace);

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "getbud.co",
            WorkspaceId = workspace.Id,
            OrganizationId = org.Id
        };
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

    private async Task<Organization> CreateTestOrganization()
    {
        var leaderId = await GetOrCreateAdminLeader();
        var orgResponse = await _adminClient.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest
            {
                Name = $"test-{Guid.NewGuid():N}.com",
                OwnerId = leaderId,
                UserEmail = "admin@getbud.co"
            });
        return (await orgResponse.Content.ReadFromJsonAsync<Organization>())!;
    }

    #region Create Tests

    [Fact]
    public async Task Create_WithPublicVisibility_ReturnsCreated()
    {
        // Arrange
        var org = await CreateTestOrganization();
        var request = new CreateWorkspaceRequest
        {
            Name = "Public WS",
            OrganizationId = org.Id,
            Visibility = Visibility.Public
        };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/workspaces", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var workspace = await response.Content.ReadFromJsonAsync<Workspace>();
        workspace.Should().NotBeNull();
        workspace!.Visibility.Should().Be(Visibility.Public);
    }

    [Fact]
    public async Task Create_WithPrivateVisibility_ReturnsCreated()
    {
        // Arrange
        var org = await CreateTestOrganization();
        var request = new CreateWorkspaceRequest
        {
            Name = "Private WS",
            OrganizationId = org.Id,
            Visibility = Visibility.Private
        };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/workspaces", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var workspace = await response.Content.ReadFromJsonAsync<Workspace>();
        workspace.Should().NotBeNull();
        workspace!.Visibility.Should().Be(Visibility.Private);
    }

    [Fact]
    public async Task Create_WithoutVisibility_ReturnsBadRequest()
    {
        // Arrange
        var org = await CreateTestOrganization();
        var request = new CreateWorkspaceRequest
        {
            Name = "Missing Visibility WS",
            OrganizationId = org.Id,
            Visibility = null
        };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/workspaces", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Visibility Filtering Tests

    [Fact]
    public async Task GetAll_AsTenantUser_FiltersPrivateWorkspaces()
    {
        // Arrange: create org, public ws, private ws, team+collaborator in public ws
        var org = await CreateTestOrganization();

        // Create public workspace
        var publicWsResponse = await _adminClient.PostAsJsonAsync("/api/workspaces",
            new CreateWorkspaceRequest
            {
                Name = "Public WS for Filter Test",
                OrganizationId = org.Id,
                Visibility = Visibility.Public
            });
        var publicWs = (await publicWsResponse.Content.ReadFromJsonAsync<Workspace>())!;

        // Create private workspace with a team and collaborator
        var privateWsResponse = await _adminClient.PostAsJsonAsync("/api/workspaces",
            new CreateWorkspaceRequest
            {
                Name = "Private WS (member) for Filter Test",
                OrganizationId = org.Id,
                Visibility = Visibility.Private
            });
        var privateWsMember = (await privateWsResponse.Content.ReadFromJsonAsync<Workspace>())!;

        // Create another private workspace (no team membership)
        var privateWsNoMemberResponse = await _adminClient.PostAsJsonAsync("/api/workspaces",
            new CreateWorkspaceRequest
            {
                Name = "Private WS (non-member) for Filter Test",
                OrganizationId = org.Id,
                Visibility = Visibility.Private
            });
        var privateWsNoMember = (await privateWsNoMemberResponse.Content.ReadFromJsonAsync<Workspace>())!;

        // Create a team in the private workspace where user IS a member
        var teamResponse = await _adminClient.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = "Test Team", WorkspaceId = privateWsMember.Id });
        var team = (await teamResponse.Content.ReadFromJsonAsync<Team>())!;

        // Create a collaborator in that team
        var collabResponse = await _adminClient.PostAsJsonAsync("/api/collaborators",
            new CreateCollaboratorRequest
            {
                FullName = "Test User",
                Email = $"testuser-{Guid.NewGuid():N}@test.com",
                Role = CollaboratorRole.IndividualContributor,
                TeamId = team.Id
            });
        var collaborator = (await collabResponse.Content.ReadFromJsonAsync<Collaborator>())!;

        // Act: get all workspaces as the non-admin tenant user
        var tenantClient = _factory.CreateTenantClient(org.Id, collaborator.Email, collaborator.Id);
        var response = await tenantClient.GetAsync($"/api/workspaces?organizationId={org.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<Workspace>>();
        result.Should().NotBeNull();

        result!.Items.Should().Contain(w => w.Id == publicWs.Id);
        result.Items.Should().Contain(w => w.Id == privateWsMember.Id);
        result.Items.Should().NotContain(w => w.Id == privateWsNoMember.Id);
    }

    #endregion

    #region Write Access Tests

    [Fact]
    public async Task Update_AsNonMember_ReturnsForbidden()
    {
        // Arrange: create org, workspace, collaborator in a different workspace
        var org = await CreateTestOrganization();

        // Create workspace
        var wsResponse = await _adminClient.PostAsJsonAsync("/api/workspaces",
            new CreateWorkspaceRequest
            {
                Name = "Target WS",
                OrganizationId = org.Id,
                Visibility = Visibility.Public
            });
        var targetWs = (await wsResponse.Content.ReadFromJsonAsync<Workspace>())!;

        // Create a DIFFERENT workspace with team and collaborator
        var otherWsResponse = await _adminClient.PostAsJsonAsync("/api/workspaces",
            new CreateWorkspaceRequest
            {
                Name = "Other WS",
                OrganizationId = org.Id,
                Visibility = Visibility.Public
            });
        var otherWs = (await otherWsResponse.Content.ReadFromJsonAsync<Workspace>())!;

        var teamResponse = await _adminClient.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = "Other Team", WorkspaceId = otherWs.Id });
        var team = (await teamResponse.Content.ReadFromJsonAsync<Team>())!;

        var collabResponse = await _adminClient.PostAsJsonAsync("/api/collaborators",
            new CreateCollaboratorRequest
            {
                FullName = "Non-Member User",
                Email = $"nonmember-{Guid.NewGuid():N}@test.com",
                Role = CollaboratorRole.IndividualContributor,
                TeamId = team.Id
            });
        var collaborator = (await collabResponse.Content.ReadFromJsonAsync<Collaborator>())!;

        // Act: try to update targetWs as a non-member
        var tenantClient = _factory.CreateTenantClient(org.Id, collaborator.Email, collaborator.Id);
        var updateRequest = new UpdateWorkspaceRequest
        {
            Name = "Should Not Update",
            Visibility = Visibility.Private
        };
        var response = await tenantClient.PutAsJsonAsync($"/api/workspaces/{targetWs.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion
}
