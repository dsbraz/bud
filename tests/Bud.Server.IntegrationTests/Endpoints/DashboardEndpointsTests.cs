using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bud.Server.Infrastructure.Persistence;
using Bud.Server.IntegrationTests.Helpers;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bud.Server.IntegrationTests.Endpoints;

public sealed class DashboardEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DashboardEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMyDashboard_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/me/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyDashboard_WithoutCollaboratorInToken_ReturnsForbidden()
    {
        var tenantUser = await GetOrCreateTenantUserAsync();
        var client = _factory.CreateClient();
        var token = JwtTestHelper.GenerateTenantUserTokenWithoutCollaborator(tenantUser.Email, tenantUser.OrganizationId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantUser.OrganizationId.ToString());

        var response = await client.GetAsync("/api/me/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Detail.Should().Be("Colaborador não identificado.");
    }

    [Fact]
    public async Task GetMyDashboard_WithValidAuthenticatedCollaborator_ReturnsOk()
    {
        var tenantUser = await GetOrCreateTenantUserAsync();
        var client = _factory.CreateTenantClient(tenantUser.OrganizationId, tenantUser.Email, tenantUser.CollaboratorId);

        var response = await client.GetAsync("/api/me/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMyDashboard_WithUnknownCollaborator_ReturnsNotFound()
    {
        var tenantUser = await GetOrCreateTenantUserAsync();
        var client = _factory.CreateTenantClient(tenantUser.OrganizationId, tenantUser.Email, Guid.NewGuid());

        var response = await client.GetAsync("/api/me/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Detail.Should().Be("Colaborador não encontrado.");
    }

    [Fact]
    public async Task GetMyDashboard_WithTeamId_ReturnsOk()
    {
        var tenantUser = await GetOrCreateTenantUserAsync();
        var teamId = await GetOrCreateTeamWithMemberAsync(tenantUser.OrganizationId);
        var client = _factory.CreateTenantClient(tenantUser.OrganizationId, tenantUser.Email, tenantUser.CollaboratorId);

        var response = await client.GetAsync($"/api/me/dashboard?teamId={teamId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMyDashboard_WithNonExistentTeamId_ReturnsOkEmpty()
    {
        var tenantUser = await GetOrCreateTenantUserAsync();
        var client = _factory.CreateTenantClient(tenantUser.OrganizationId, tenantUser.Email, tenantUser.CollaboratorId);

        var response = await client.GetAsync($"/api/me/dashboard?teamId={Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<Guid> GetOrCreateTeamWithMemberAsync(Guid organizationId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var workspace = await dbContext.Workspaces
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.OrganizationId == organizationId);

        if (workspace is null)
        {
            workspace = new Workspace
            {
                Id = Guid.NewGuid(),
                Name = "WS Filter Test",
                OrganizationId = organizationId
            };
            dbContext.Workspaces.Add(workspace);
        }

        var teamId = Guid.NewGuid();

        var leader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Dashboard Filter Leader",
            Email = $"filter-leader-{teamId:N}@test.com",
            Role = Bud.Server.Domain.Model.CollaboratorRole.Leader,
            TeamId = null,
            OrganizationId = organizationId
        };
        dbContext.Collaborators.Add(leader);
        await dbContext.SaveChangesAsync();

        var team = new Team
        {
            Id = teamId,
            Name = "Dashboard Filter Team",
            WorkspaceId = workspace.Id,
            OrganizationId = organizationId,
            LeaderId = leader.Id
        };
        dbContext.Teams.Add(team);

        var member = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Team Member",
            Email = $"team-member-{teamId:N}@test.com",
            OrganizationId = organizationId
        };
        dbContext.Collaborators.Add(member);

        dbContext.Set<CollaboratorTeam>().Add(new CollaboratorTeam
        {
            CollaboratorId = member.Id,
            TeamId = teamId
        });

        await dbContext.SaveChangesAsync();
        return teamId;
    }

    private async Task<(Guid OrganizationId, Guid CollaboratorId, string Email)> GetOrCreateTenantUserAsync()
    {
        const string email = "admin@getbud.co";

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var collaborator = await dbContext.Collaborators
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Email == email);

        if (collaborator is not null)
        {
            return (collaborator.OrganizationId, collaborator.Id, collaborator.Email);
        }

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "dashboard-test.com"
        };
        dbContext.Organizations.Add(org);

        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Principal",
            OrganizationId = org.Id
        };
        dbContext.Workspaces.Add(workspace);

        var leader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Administrador Dashboard",
            Email = email,
            Role = Bud.Server.Domain.Model.CollaboratorRole.Leader,
            TeamId = null,
            OrganizationId = org.Id
        };
        dbContext.Collaborators.Add(leader);
        await dbContext.SaveChangesAsync();

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Time Dashboard",
            WorkspaceId = workspace.Id,
            OrganizationId = org.Id,
            LeaderId = leader.Id
        };
        dbContext.Teams.Add(team);

        leader.TeamId = team.Id;

        await dbContext.SaveChangesAsync();

        org.OwnerId = leader.Id;
        await dbContext.SaveChangesAsync();

        return (org.Id, leader.Id, leader.Email);
    }
}
