using System.Net;
using System.Net.Http.Json;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bud.Server.IntegrationTests.Endpoints;

public class OrganizationsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public OrganizationsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAdminClient();
    }

    private async Task<Guid> GetOrCreateAdminLeader()
    {
        // Create bootstrap hierarchy similar to DbSeeder
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Bud.Server.Data.ApplicationDbContext>();

        // Check if admin leader already exists
        var existingLeader = await dbContext.Collaborators
            .FirstOrDefaultAsync(c => c.Email == "admin@getbud.co");

        if (existingLeader != null)
        {
            return existingLeader.Id;
        }

        // Create hierarchy: Org -> Workspace -> Team -> Leader
        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "getbud.co",
            OwnerId = null
        };
        dbContext.Organizations.Add(org);

        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Bud",
            OrganizationId = org.Id
        };
        dbContext.Workspaces.Add(workspace);

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Bud",
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

        // Update org with owner
        org.OwnerId = adminLeader.Id;
        await dbContext.SaveChangesAsync();

        return adminLeader.Id;
    }

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var leaderId = await GetOrCreateAdminLeader();
        var request = new CreateOrganizationRequest
        {
            Name = "test-org.com",
            OwnerId = leaderId,
            UserEmail = "admin@getbud.co"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/organizations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var organization = await response.Content.ReadFromJsonAsync<Organization>();
        organization.Should().NotBeNull();
        organization!.Name.Should().Be("test-org.com");
        organization.Id.Should().NotBeEmpty();
        organization.OwnerId.Should().Be(leaderId);
    }

    [Fact]
    public async Task Create_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var leaderId = await GetOrCreateAdminLeader();
        var request = new CreateOrganizationRequest
        {
            Name = "",
            OwnerId = leaderId,
            UserEmail = "admin@getbud.co"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/organizations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetById_WithExistingId_ReturnsOk()
    {
        // Arrange
        var leaderId = await GetOrCreateAdminLeader();
        var createRequest = new CreateOrganizationRequest
        {
            Name = "getbyid-test.com",
            OwnerId = leaderId,
            UserEmail = "admin@getbud.co"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/organizations", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<Organization>();

        // Act
        var response = await _client.GetAsync($"/api/organizations/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var organization = await response.Content.ReadFromJsonAsync<Organization>();
        organization.Should().NotBeNull();
        organization!.Id.Should().Be(created.Id);
        organization.Name.Should().Be("getbyid-test.com");
    }

    [Fact]
    public async Task GetById_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/organizations/{nonExistingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_ReturnsPagedResult()
    {
        // Arrange
        var leaderId = await GetOrCreateAdminLeader();
        await _client.PostAsJsonAsync("/api/organizations", new CreateOrganizationRequest
        {
            Name = "org1.com",
            OwnerId = leaderId,
            UserEmail = "admin@getbud.co"
        });
        await _client.PostAsJsonAsync("/api/organizations", new CreateOrganizationRequest
        {
            Name = "org2.com",
            OwnerId = leaderId,
            UserEmail = "admin@getbud.co"
        });

        // Act
        var response = await _client.GetAsync("/api/organizations?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<Organization>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task Update_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var leaderId = await GetOrCreateAdminLeader();
        var createRequest = new CreateOrganizationRequest
        {
            Name = "original.com",
            OwnerId = leaderId,
            UserEmail = "admin@getbud.co"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/organizations", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<Organization>();

        var updateRequest = new UpdateOrganizationRequest { Name = "updated.com" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/organizations/{created!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<Organization>();
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("updated.com");
    }

    [Fact]
    public async Task Update_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var updateRequest = new UpdateOrganizationRequest { Name = "updated.com" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/organizations/{nonExistingId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WithExistingId_ReturnsNoContent()
    {
        // Arrange
        var leaderId = await GetOrCreateAdminLeader();
        var createRequest = new CreateOrganizationRequest
        {
            Name = "to-delete.com",
            OwnerId = leaderId,
            UserEmail = "admin@getbud.co"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/organizations", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<Organization>();

        // Act
        var response = await _client.DeleteAsync($"/api/organizations/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's deleted
        var getResponse = await _client.GetAsync($"/api/organizations/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/organizations/{nonExistingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
