using System.Net;
using System.Net.Http.Json;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Xunit;

namespace Bud.Server.IntegrationTests.Endpoints;

public class OrganizationsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OrganizationsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new CreateOrganizationRequest { Name = "Test Organization" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/organizations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var organization = await response.Content.ReadFromJsonAsync<Organization>();
        organization.Should().NotBeNull();
        organization!.Name.Should().Be("Test Organization");
        organization.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Create_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateOrganizationRequest { Name = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/organizations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetById_WithExistingId_ReturnsOk()
    {
        // Arrange
        var createRequest = new CreateOrganizationRequest { Name = "Test Organization for GetById" };
        var createResponse = await _client.PostAsJsonAsync("/api/organizations", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<Organization>();

        // Act
        var response = await _client.GetAsync($"/api/organizations/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var organization = await response.Content.ReadFromJsonAsync<Organization>();
        organization.Should().NotBeNull();
        organization!.Id.Should().Be(created.Id);
        organization.Name.Should().Be("Test Organization for GetById");
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
        await _client.PostAsJsonAsync("/api/organizations", new CreateOrganizationRequest { Name = "Org 1" });
        await _client.PostAsJsonAsync("/api/organizations", new CreateOrganizationRequest { Name = "Org 2" });

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
        var createRequest = new CreateOrganizationRequest { Name = "Original Name" };
        var createResponse = await _client.PostAsJsonAsync("/api/organizations", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<Organization>();

        var updateRequest = new UpdateOrganizationRequest { Name = "Updated Name" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/organizations/{created!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<Organization>();
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task Update_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var updateRequest = new UpdateOrganizationRequest { Name = "Updated Name" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/organizations/{nonExistingId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WithExistingId_ReturnsNoContent()
    {
        // Arrange
        var createRequest = new CreateOrganizationRequest { Name = "Organization to Delete" };
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
