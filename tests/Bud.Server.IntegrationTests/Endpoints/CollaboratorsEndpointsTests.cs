using System.Net;
using System.Net.Http.Json;
using Bud.Shared.Contracts;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bud.Server.IntegrationTests.Endpoints;

public class CollaboratorsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _adminClient;

    public CollaboratorsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _adminClient = factory.CreateGlobalAdminClient();
    }

    [Fact]
    public async Task GetAll_WithInvalidPageSize_ReturnsBadRequest()
    {
        var response = await _adminClient.GetAsync("/api/collaborators?page=1&pageSize=101");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Detail.Should().Be("O parâmetro 'pageSize' deve estar entre 1 e 100.");
    }

    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
    {
        var unauthenticatedClient = _factory.CreateClient();

        var response = await unauthenticatedClient.GetAsync("/api/collaborators");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_AsNonOwner_ReturnsForbidden()
    {
        var leaderId = await GetOrCreateAdminLeader();
        var orgResponse = await _adminClient.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest
            {
                Name = $"test-{Guid.NewGuid():N}.com",
                OwnerId = leaderId
            });
        var org = (await orgResponse.Content.ReadFromJsonAsync<Organization>())!;

        var collaborator = await CreateNonOwnerCollaborator(org.Id);
        var tenantClient = _factory.CreateTenantClient(org.Id, collaborator.Email, collaborator.Id);

        var request = new CreateCollaboratorRequest
        {
            FullName = "Novo Colaborador",
            Email = $"novo-{Guid.NewGuid():N}@test.com",
            Role = Bud.Shared.Contracts.CollaboratorRole.IndividualContributor
        };

        var response = await tenantClient.PostAsJsonAsync("/api/collaborators", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_AsNonOwner_ReturnsForbidden()
    {
        var leaderId = await GetOrCreateAdminLeader();
        var orgResponse = await _adminClient.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest
            {
                Name = $"test-{Guid.NewGuid():N}.com",
                OwnerId = leaderId
            });
        var org = (await orgResponse.Content.ReadFromJsonAsync<Organization>())!;

        var nonOwner = await CreateNonOwnerCollaborator(org.Id);
        var target = await CreateCollaborator(org.Id);
        var tenantClient = _factory.CreateTenantClient(org.Id, nonOwner.Email, nonOwner.Id);

        var request = new PatchCollaboratorRequest
        {
            FullName = "Colaborador Atualizado",
            Email = $"atualizado-{Guid.NewGuid():N}@test.com",
            Role = Bud.Shared.Contracts.CollaboratorRole.IndividualContributor
        };

        var response = await tenantClient.PatchAsJsonAsync($"/api/collaborators/{target.Id}", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_WithAssociatedMissions_ReturnsConflict()
    {
        // Arrange
        var leaderId = await GetOrCreateAdminLeader();
        var orgResponse = await _adminClient.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest
            {
                Name = $"test-{Guid.NewGuid():N}.com",
                OwnerId = leaderId
            });
        var org = (await orgResponse.Content.ReadFromJsonAsync<Organization>())!;

        var collaborator = await CreateCollaborator(org.Id);

        // Create mission scoped to collaborator via DbContext
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Bud.Server.Infrastructure.Persistence.ApplicationDbContext>();
        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Missão Colaborador",
            OrganizationId = org.Id,
            CollaboratorId = collaborator.Id,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = Bud.Server.Domain.Model.MissionStatus.Active
        };
        dbContext.Missions.Add(mission);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await _adminClient.DeleteAsync($"/api/collaborators/{collaborator.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_AsNonOwner_ReturnsForbidden()
    {
        var leaderId = await GetOrCreateAdminLeader();
        var orgResponse = await _adminClient.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest
            {
                Name = $"test-{Guid.NewGuid():N}.com",
                OwnerId = leaderId
            });
        var org = (await orgResponse.Content.ReadFromJsonAsync<Organization>())!;

        var nonOwner = await CreateNonOwnerCollaborator(org.Id);
        var target = await CreateCollaborator(org.Id);
        var tenantClient = _factory.CreateTenantClient(org.Id, nonOwner.Email, nonOwner.Id);

        var response = await tenantClient.DeleteAsync($"/api/collaborators/{target.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSubordinates_ValidLeader_ReturnsOk()
    {
        var leaderId = await GetOrCreateAdminLeader();
        var orgResponse = await _adminClient.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest
            {
                Name = $"test-{Guid.NewGuid():N}.com",
                OwnerId = leaderId
            });
        var org = (await orgResponse.Content.ReadFromJsonAsync<Organization>())!;

        var leader = await CreateLeaderCollaborator(org.Id);
        var sub = await CreateSubordinate(org.Id, leader.Id);

        var response = await _adminClient.GetAsync($"/api/collaborators/{leader.Id}/subordinates");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var nodes = await response.Content.ReadFromJsonAsync<List<CollaboratorHierarchyNodeDto>>();
        nodes.Should().NotBeNull();
        nodes.Should().HaveCount(1);
        nodes![0].FullName.Should().Be(sub.FullName);
    }

    [Fact]
    public async Task GetSubordinates_NonExistent_ReturnsNotFound()
    {
        var response = await _adminClient.GetAsync($"/api/collaborators/{Guid.NewGuid()}/subordinates");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSubordinates_Recursive_ReturnsNestedTree()
    {
        var leaderId = await GetOrCreateAdminLeader();
        var orgResponse = await _adminClient.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest
            {
                Name = $"test-{Guid.NewGuid():N}.com",
                OwnerId = leaderId
            });
        var org = (await orgResponse.Content.ReadFromJsonAsync<Organization>())!;

        var topLeader = await CreateLeaderCollaborator(org.Id);
        var midLeader = await CreateLeaderSubordinate(org.Id, topLeader.Id);
        var contributor = await CreateSubordinate(org.Id, midLeader.Id);

        var response = await _adminClient.GetAsync($"/api/collaborators/{topLeader.Id}/subordinates");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var nodes = await response.Content.ReadFromJsonAsync<List<CollaboratorHierarchyNodeDto>>();
        nodes.Should().HaveCount(1);
        nodes![0].FullName.Should().Be(midLeader.FullName);
        nodes[0].Children.Should().HaveCount(1);
        nodes[0].Children[0].FullName.Should().Be(contributor.FullName);
    }

    private async Task<Collaborator> CreateLeaderCollaborator(Guid organizationId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Bud.Server.Infrastructure.Persistence.ApplicationDbContext>();

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = $"Líder {Guid.NewGuid():N}",
            Email = $"leader-{Guid.NewGuid():N}@test.com",
            Role = Bud.Server.Domain.Model.CollaboratorRole.Leader,
            OrganizationId = organizationId
        };

        dbContext.Collaborators.Add(collaborator);
        await dbContext.SaveChangesAsync();

        return collaborator;
    }

    private async Task<Collaborator> CreateLeaderSubordinate(Guid organizationId, Guid leaderId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Bud.Server.Infrastructure.Persistence.ApplicationDbContext>();

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = $"Sub-líder {Guid.NewGuid():N}",
            Email = $"subleader-{Guid.NewGuid():N}@test.com",
            Role = Bud.Server.Domain.Model.CollaboratorRole.Leader,
            OrganizationId = organizationId,
            LeaderId = leaderId
        };

        dbContext.Collaborators.Add(collaborator);
        await dbContext.SaveChangesAsync();

        return collaborator;
    }

    private async Task<Collaborator> CreateSubordinate(Guid organizationId, Guid leaderId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Bud.Server.Infrastructure.Persistence.ApplicationDbContext>();

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = $"Liderado {Guid.NewGuid():N}",
            Email = $"sub-{Guid.NewGuid():N}@test.com",
            Role = Bud.Server.Domain.Model.CollaboratorRole.IndividualContributor,
            OrganizationId = organizationId,
            LeaderId = leaderId
        };

        dbContext.Collaborators.Add(collaborator);
        await dbContext.SaveChangesAsync();

        return collaborator;
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

    private async Task<Collaborator> CreateNonOwnerCollaborator(Guid organizationId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Bud.Server.Infrastructure.Persistence.ApplicationDbContext>();

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Colaborador Teste",
            Email = $"colaborador-{Guid.NewGuid():N}@test.com",
            Role = Bud.Server.Domain.Model.CollaboratorRole.IndividualContributor,
            OrganizationId = organizationId
        };

        dbContext.Collaborators.Add(collaborator);
        await dbContext.SaveChangesAsync();

        return collaborator;
    }

    private async Task<Collaborator> CreateCollaborator(Guid organizationId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Bud.Server.Infrastructure.Persistence.ApplicationDbContext>();

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Colaborador Alvo",
            Email = $"alvo-{Guid.NewGuid():N}@test.com",
            Role = Bud.Server.Domain.Model.CollaboratorRole.IndividualContributor,
            OrganizationId = organizationId
        };

        dbContext.Collaborators.Add(collaborator);
        await dbContext.SaveChangesAsync();

        return collaborator;
    }
}
