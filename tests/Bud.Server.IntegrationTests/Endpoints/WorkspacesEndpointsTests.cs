using System.Net;
using System.Net.Http.Json;
using System.Diagnostics;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
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
        _adminClient = factory.CreateGlobalAdminClient();
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

        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "getbud.co",
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
            });
        return (await orgResponse.Content.ReadFromJsonAsync<Organization>())!;
    }

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

    #region Create Tests

    [Fact]
    public async Task GetAll_WithInvalidPageSize_ReturnsBadRequest()
    {
        // Act
        var response = await _adminClient.GetAsync("/api/workspaces?page=1&pageSize=101");

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

        var response = await _adminClient.GetAsync("/api/workspaces?page=1&pageSize=20");

        watch.Stop();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        watch.ElapsedMilliseconds.Should().BeLessThan(5000);
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreated()
    {
        // Arrange
        var org = await CreateTestOrganization();
        var request = new CreateWorkspaceRequest
        {
            Name = "Test Workspace",
            OrganizationId = org.Id
        };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/workspaces", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var workspace = await response.Content.ReadFromJsonAsync<Workspace>();
        workspace.Should().NotBeNull();
        workspace!.Name.Should().Be("Test Workspace");
    }

    [Fact]
    public async Task Create_AsNonOwner_ReturnsForbidden()
    {
        // Arrange
        var org = await CreateTestOrganization();
        var collaborator = await CreateNonOwnerCollaborator(org.Id);
        var tenantClient = _factory.CreateTenantClient(org.Id, collaborator.Email, collaborator.Id);

        var request = new CreateWorkspaceRequest
        {
            Name = "WS NonOwner",
            OrganizationId = org.Id
        };

        // Act
        var response = await tenantClient.PostAsJsonAsync("/api/workspaces", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Tenant Isolation Tests

    [Fact]
    public async Task GetAll_WithTenantClient_ShouldNotReturnWorkspacesFromAnotherTenant()
    {
        var orgA = await CreateTestOrganization();
        var orgB = await CreateTestOrganization();

        var wsAName = $"WS-A-{Guid.NewGuid():N}";
        var wsBName = $"WS-B-{Guid.NewGuid():N}";

        (await _adminClient.PostAsJsonAsync("/api/workspaces",
            new CreateWorkspaceRequest { Name = wsAName, OrganizationId = orgA.Id }))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        (await _adminClient.PostAsJsonAsync("/api/workspaces",
            new CreateWorkspaceRequest { Name = wsBName, OrganizationId = orgB.Id }))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var collaboratorA = await CreateNonOwnerCollaborator(orgA.Id);
        var tenantClientA = _factory.CreateTenantClient(orgA.Id, collaboratorA.Email, collaboratorA.Id);

        var visibleResponse = await tenantClientA.GetAsync($"/api/workspaces?search={wsAName}");
        var hiddenResponse = await tenantClientA.GetAsync($"/api/workspaces?search={wsBName}");

        visibleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        hiddenResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var visible = await visibleResponse.Content.ReadFromJsonAsync<PagedResult<Workspace>>();
        var hidden = await hiddenResponse.Content.ReadFromJsonAsync<PagedResult<Workspace>>();

        visible.Should().NotBeNull();
        hidden.Should().NotBeNull();
        visible!.Items.Should().ContainSingle(w => w.Name == wsAName);
        hidden!.Items.Should().BeEmpty();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidData_ReturnsOk()
    {
        // Arrange
        var org = await CreateTestOrganization();
        var createResponse = await _adminClient.PostAsJsonAsync("/api/workspaces",
            new CreateWorkspaceRequest
            {
                Name = "Original Name",
                OrganizationId = org.Id
            });
        var workspace = (await createResponse.Content.ReadFromJsonAsync<Workspace>())!;

        var updateRequest = new UpdateWorkspaceRequest
        {
            Name = "Updated Name"
        };

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/workspaces/{workspace.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<Workspace>();
        updated!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task Update_AsNonOwner_ReturnsForbidden()
    {
        // Arrange
        var org = await CreateTestOrganization();

        var wsResponse = await _adminClient.PostAsJsonAsync("/api/workspaces",
            new CreateWorkspaceRequest
            {
                Name = "Target WS",
                OrganizationId = org.Id
            });
        var targetWs = (await wsResponse.Content.ReadFromJsonAsync<Workspace>())!;

        var collaborator = await CreateNonOwnerCollaborator(org.Id);
        var tenantClient = _factory.CreateTenantClient(org.Id, collaborator.Email, collaborator.Id);

        var updateRequest = new UpdateWorkspaceRequest
        {
            Name = "Should Not Update"
        };

        // Act
        var response = await tenantClient.PutAsJsonAsync($"/api/workspaces/{targetWs.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ExistingWorkspace_ReturnsNoContent()
    {
        // Arrange
        var org = await CreateTestOrganization();
        var createResponse = await _adminClient.PostAsJsonAsync("/api/workspaces",
            new CreateWorkspaceRequest
            {
                Name = "WS to Delete",
                OrganizationId = org.Id
            });
        var workspace = (await createResponse.Content.ReadFromJsonAsync<Workspace>())!;

        // Act
        var response = await _adminClient.DeleteAsync($"/api/workspaces/{workspace.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    #endregion

    #region Authentication Tests

    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var unauthenticatedClient = _factory.CreateClient();

        // Act
        var response = await unauthenticatedClient.GetAsync("/api/workspaces");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}
