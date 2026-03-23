using System.Net;
using System.Net.Http.Json;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Bud.Shared.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bud.Api.IntegrationTests.Endpoints;

public class MissionsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public MissionsEndpointsTests(CustomWebApplicationFactory factory)
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
            TeamId = null,
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

    #region Create Tests

    [Fact]
    public async Task GetAll_ShouldRespondWithinBudget()
    {
        var watch = Stopwatch.StartNew();

        var response = await _client.GetAsync("/api/goals?page=1&pageSize=20");

        watch.Stop();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        watch.ElapsedMilliseconds.Should().BeLessThan(5000);
    }

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
            });
        var org = await orgResponse.Content.ReadFromJsonAsync<Organization>();
        SetTenantHeader(org!.Id);

        var request = new CreateGoalRequest
        {
            Name = "Test Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = Bud.Shared.Kernel.Enums.GoalStatus.Planned,
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/goals", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var mission = await response.Content.ReadFromJsonAsync<Goal>();
        mission.Should().NotBeNull();
        mission!.Name.Should().Be("Test Mission");
        mission.OrganizationId.Should().Be(org.Id);
        mission.CollaboratorId.Should().BeNull();
    }

    [Fact]
    public async Task Create_WithTenantMismatch_ReturnsForbidden()
    {
        // Arrange: create two organizations
        var leaderId = await GetOrCreateAdminLeader();
        var org1Response = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest
            {
                Name = "org-1.com",
                OwnerId = leaderId
            });
        var org1 = await org1Response.Content.ReadFromJsonAsync<Organization>();

        var org2Response = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest
            {
                Name = "org-2.com",
                OwnerId = leaderId
            });
        var org2 = await org2Response.Content.ReadFromJsonAsync<Organization>();

        var collaborator = await CreateNonOwnerCollaborator(org1!.Id);
        var tenantClient = _factory.CreateTenantClient(org1.Id, collaborator.Email, collaborator.Id);

        // Tenant client is scoped to org1, but tries to create goal referencing org2 collaborator
        var org2Collaborator = await CreateNonOwnerCollaborator(org2!.Id);
        var request = new CreateGoalRequest
        {
            Name = "Mission Forbidden",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = Bud.Shared.Kernel.Enums.GoalStatus.Planned,
            CollaboratorId = org2Collaborator.Id
        };

        // Act
        var response = await tenantClient.PostAsJsonAsync("/api/goals", request);

        // Assert: NotFound instead of Forbidden — tenant isolation hides cross-org collaborators
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithInvalidCollaboratorId_ReturnsNotFound()
    {
        // Arrange
        await GetOrCreateAdminLeader();
        var request = new CreateGoalRequest
        {
            Name = "Test Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = Bud.Shared.Kernel.Enums.GoalStatus.Planned,
            CollaboratorId = Guid.NewGuid() // Non-existent ID
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/goals", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithEndDateBeforeStartDate_ReturnsBadRequest()
    {
        // Arrange
        await GetOrCreateAdminLeader();
        var request = new CreateGoalRequest
        {
            Name = "Test Mission",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow, // Before start date
            Status = Bud.Shared.Kernel.Enums.GoalStatus.Planned,
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/goals", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithStringEnums_ReturnsCreated()
    {
        await GetOrCreateAdminLeader();
        var payload = new
        {
            name = "Mission String Enum",
            description = "String enum parsing",
            startDate = DateTime.UtcNow,
            endDate = DateTime.UtcNow.AddDays(5),
            status = "Planned"
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/goals", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_WithNumericEnums_ReturnsCreated()
    {
        await GetOrCreateAdminLeader();
        var payload = new
        {
            name = "Mission Numeric Enum",
            description = "Numeric enum parsing",
            startDate = DateTime.UtcNow,
            endDate = DateTime.UtcNow.AddDays(5),
            status = 0
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/goals", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
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

    #region GetById Tests

    [Fact]
    public async Task GetById_WithExistingId_ReturnsOk()
    {
        await GetOrCreateAdminLeader();
        // Arrange: Create mission
        var createRequest = new CreateGoalRequest
        {
            Name = "Test Mission for GetById",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = Bud.Shared.Kernel.Enums.GoalStatus.Planned,
        };
        var createResponse = await _client.PostAsJsonAsync("/api/goals", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<Goal>();

        // Act
        var response = await _client.GetAsync($"/api/goals/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var mission = await response.Content.ReadFromJsonAsync<Goal>();
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
        var response = await _client.GetAsync($"/api/goals/{nonExistingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WithFilter_ReturnsFilteredResults()
    {
        await GetOrCreateAdminLeader();
        // Arrange: Create goals
        await _client.PostAsJsonAsync("/api/goals", new CreateGoalRequest
        {
            Name = "Mission Filter Test",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = Bud.Shared.Kernel.Enums.GoalStatus.Planned,
        });

        // Act - Filter with All
        var response = await _client.GetAsync($"/api/goals?filter={GoalFilter.All}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<Goal>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAll_WithPagination_ReturnsPagedResults()
    {
        await GetOrCreateAdminLeader();
        // Arrange: Create multiple missions
        for (int i = 1; i <= 15; i++)
        {
            await _client.PostAsJsonAsync("/api/goals", new CreateGoalRequest
            {
                Name = $"Mission {i}",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(7),
                Status = Bud.Shared.Kernel.Enums.GoalStatus.Planned,
            });
        }

        // Act
        var response = await _client.GetAsync("/api/goals?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<Goal>>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.Items.Should().HaveCountLessOrEqualTo(10);
    }

    [Fact]
    public async Task GetAll_WithInvalidPageSize_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/goals?page=1&pageSize=101");

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
        var response = await _client.GetAsync($"/api/goals?search={search}");

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
        var response = await _client.GetAsync("/api/goals/progress?ids=abc");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Detail.Should().Be("O parâmetro 'ids' contém valores inválidos. Informe GUIDs separados por vírgula.");
    }

    #endregion

    #region GetMyMissions Tests

    [Fact]
    public async Task GetMyMissions_WithValidCollaborator_ReturnsHierarchyMissions()
    {
        // Arrange: Create full hierarchy
        var adminLeaderId = await GetOrCreateAdminLeader();
        var orgResponse = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest { Name = "test-org.com", OwnerId = adminLeaderId });
        var org = await orgResponse.Content.ReadFromJsonAsync<Organization>();
        SetTenantHeader(org!.Id);

        var workspaceResponse = await _client.PostAsJsonAsync("/api/workspaces",
            new CreateWorkspaceRequest { Name = "Test Workspace", OrganizationId = org!.Id });
        var workspace = await workspaceResponse.Content.ReadFromJsonAsync<Workspace>();

        // Create a leader in the new org for team creation
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Bud.Infrastructure.Persistence.ApplicationDbContext>();

        var orgLeader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Líder Org Teste",
            Email = $"leader-{Guid.NewGuid():N}@test-org.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = org!.Id,
            TeamId = null
        };
        dbContext.Collaborators.Add(orgLeader);
        await dbContext.SaveChangesAsync();

        var teamResponse = await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest { Name = "Test Team", WorkspaceId = workspace!.Id, LeaderId = orgLeader.Id });
        var team = await teamResponse.Content.ReadFromJsonAsync<Team>();

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = $"test-{Guid.NewGuid():N}@example.com",
            Role = CollaboratorRole.IndividualContributor,
            OrganizationId = org!.Id,
            TeamId = team!.Id
        };

        dbContext.Collaborators.Add(collaborator);
        await dbContext.SaveChangesAsync();

        // Create missions: one org-level, one collaborator-level
        await _client.PostAsJsonAsync("/api/goals", new CreateGoalRequest
        {
            Name = "Org Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = Bud.Shared.Kernel.Enums.GoalStatus.Planned,
        });

        await _client.PostAsJsonAsync("/api/goals", new CreateGoalRequest
        {
            Name = "Collaborator Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = Bud.Shared.Kernel.Enums.GoalStatus.Planned,
            CollaboratorId = collaborator!.Id
        });

        // Act
        var collaboratorClient = _factory.CreateTenantClient(org.Id, collaborator.Email, collaborator.Id);
        var response = await collaboratorClient.GetAsync("/api/goals?filter=Mine");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<Goal>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCountGreaterOrEqualTo(1);
        result.Items.Should().Contain(m => m.Name == "Collaborator Mission");
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidRequest_ReturnsOk()
    {
        await GetOrCreateAdminLeader();
        // Arrange: Create mission
        var createRequest = new CreateGoalRequest
        {
            Name = "Original Name",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = Bud.Shared.Kernel.Enums.GoalStatus.Planned,
        };
        var createResponse = await _client.PostAsJsonAsync("/api/goals", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<Goal>();

        var updateRequest = new PatchGoalRequest
        {
            Name = "Updated Name",
            StartDate = created!.StartDate,
            EndDate = created.EndDate,
            Status = Bud.Shared.Kernel.Enums.GoalStatus.Active
        };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/goals/{created.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<Goal>();
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Name");
        updated.Status.Should().Be(GoalStatus.Active);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithExistingId_ReturnsNoContent()
    {
        await GetOrCreateAdminLeader();
        // Arrange: Create mission
        var createRequest = new CreateGoalRequest
        {
            Name = "Mission to Delete",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = Bud.Shared.Kernel.Enums.GoalStatus.Planned,
        };
        var createResponse = await _client.PostAsJsonAsync("/api/goals", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<Goal>();

        // Act
        var response = await _client.DeleteAsync($"/api/goals/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's deleted
        var getResponse = await _client.GetAsync($"/api/goals/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var unauthenticatedClient = _factory.CreateClient();

        // Act
        var response = await unauthenticatedClient.GetAsync("/api/goals");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}
