using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bud.Server.Data;
using Bud.Server.IntegrationTests.Helpers;
using Bud.Shared.Models;
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

        var response = await client.GetAsync("/api/dashboard/my-dashboard");

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

        var response = await client.GetAsync("/api/dashboard/my-dashboard");

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

        var response = await client.GetAsync("/api/dashboard/my-dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMyDashboard_WithUnknownCollaborator_ReturnsNotFound()
    {
        var tenantUser = await GetOrCreateTenantUserAsync();
        var client = _factory.CreateTenantClient(tenantUser.OrganizationId, tenantUser.Email, Guid.NewGuid());

        var response = await client.GetAsync("/api/dashboard/my-dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Detail.Should().Be("Colaborador não encontrado.");
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

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Time Dashboard",
            WorkspaceId = workspace.Id,
            OrganizationId = org.Id
        };
        dbContext.Teams.Add(team);

        var leader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Administrador Dashboard",
            Email = email,
            Role = CollaboratorRole.Leader,
            TeamId = team.Id,
            OrganizationId = org.Id
        };
        dbContext.Collaborators.Add(leader);

        await dbContext.SaveChangesAsync();

        org.OwnerId = leader.Id;
        await dbContext.SaveChangesAsync();

        return (org.Id, leader.Id, leader.Email);
    }
}
