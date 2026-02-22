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

public class MissionObjectivesEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public MissionObjectivesEndpointsTests(CustomWebApplicationFactory factory)
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

    private async Task<Mission> CreateTestMission()
    {
        var leaderId = await GetOrCreateAdminLeader();
        var orgResponse = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest
            {
                Name = $"obj-test-org-{Guid.NewGuid():N}.com",
                OwnerId = leaderId,
            });
        var org = await orgResponse.Content.ReadFromJsonAsync<Organization>();

        var missionResponse = await _client.PostAsJsonAsync("/api/missions",
            new CreateMissionRequest
            {
                Name = "Test Mission",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(7),
                Status = Bud.Shared.Contracts.MissionStatus.Planned,
                ScopeType = Bud.Shared.Contracts.MissionScopeType.Organization,
                ScopeId = org!.Id
            });

        return (await missionResponse.Content.ReadFromJsonAsync<Mission>())!;
    }

    #region Create Tests

    [Fact]
    public async Task Create_WithValidData_ReturnsCreated()
    {
        var mission = await CreateTestMission();
        var request = new CreateObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Objetivo Estratégico",
            Description = "Descrição do objetivo",
            Dimension = "Clientes"
        };

        var response = await _client.PostAsJsonAsync("/api/objectives", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var objective = await response.Content.ReadFromJsonAsync<Objective>();
        objective.Should().NotBeNull();
        objective!.Name.Should().Be("Objetivo Estratégico");
        objective.Description.Should().Be("Descrição do objetivo");
        objective.Dimension.Should().Be("Clientes");
        objective.MissionId.Should().Be(mission.Id);
        objective.OrganizationId.Should().Be(mission.OrganizationId);
    }

    [Fact]
    public async Task Create_WithInvalidMission_ReturnsNotFound()
    {
        var request = new CreateObjectiveRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Objetivo"
        };

        var response = await _client.PostAsJsonAsync("/api/objectives", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithEmptyName_ReturnsBadRequest()
    {
        var mission = await CreateTestMission();

        var request = new CreateObjectiveRequest
        {
            MissionId = mission.Id,
            Name = ""
        };

        var response = await _client.PostAsJsonAsync("/api/objectives", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidData_ReturnsOk()
    {
        var mission = await CreateTestMission();
        var createResponse = await _client.PostAsJsonAsync("/api/objectives",
            new CreateObjectiveRequest { MissionId = mission.Id, Name = "Original" });
        var created = await createResponse.Content.ReadFromJsonAsync<Objective>();

        var updateRequest = new PatchObjectiveRequest
        {
            Name = "Atualizado",
            Description = "Nova descrição",
            Dimension = "Clientes"
        };

        var response = await _client.PatchAsJsonAsync($"/api/objectives/{created!.Id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<Objective>();
        updated!.Name.Should().Be("Atualizado");
        updated.Description.Should().Be("Nova descrição");
        updated.Dimension.Should().Be("Clientes");
    }

    [Fact]
    public async Task Update_WithNonExistentId_ReturnsNotFound()
    {
        var response = await _client.PatchAsJsonAsync($"/api/objectives/{Guid.NewGuid()}",
            new PatchObjectiveRequest { Name = "X" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithNoChildren_ReturnsNoContent()
    {
        var mission = await CreateTestMission();

        var createResponse = await _client.PostAsJsonAsync("/api/objectives",
            new CreateObjectiveRequest { MissionId = mission.Id, Name = "Objetivo" });
        var created = await createResponse.Content.ReadFromJsonAsync<Objective>();

        var response = await _client.DeleteAsync($"/api/objectives/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_WithNonExistentId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/api/objectives/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ReturnsOk()
    {
        var mission = await CreateTestMission();

        var createResponse = await _client.PostAsJsonAsync("/api/objectives",
            new CreateObjectiveRequest { MissionId = mission.Id, Name = "Objetivo" });
        var created = await createResponse.Content.ReadFromJsonAsync<Objective>();

        var response = await _client.GetAsync($"/api/objectives/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var objective = await response.Content.ReadFromJsonAsync<Objective>();
        objective!.Name.Should().Be("Objetivo");
    }

    [Fact]
    public async Task GetById_WithNonExistentId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/objectives/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsAllObjectivesFromMission()
    {
        var mission = await CreateTestMission();

        await _client.PostAsJsonAsync("/api/objectives",
            new CreateObjectiveRequest { MissionId = mission.Id, Name = "Objetivo A" });

        await _client.PostAsJsonAsync("/api/objectives",
            new CreateObjectiveRequest { MissionId = mission.Id, Name = "Objetivo B" });

        var response = await _client.GetAsync($"/api/objectives?missionId={mission.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<Objective>>();
        result!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_WithInvalidPageSize_ReturnsBadRequest()
    {
        var response = await _client.GetAsync($"/api/objectives?missionId={Guid.NewGuid()}&page=1&pageSize=101");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Detail.Should().Be("O parâmetro 'pageSize' deve estar entre 1 e 100.");
    }

    #endregion

    #region Convenience Endpoint Tests

    [Fact]
    public async Task GetObjectives_ViaMissionsEndpoint_ReturnsOk()
    {
        var mission = await CreateTestMission();

        await _client.PostAsJsonAsync("/api/objectives",
            new CreateObjectiveRequest { MissionId = mission.Id, Name = "Objetivo via Missão" });

        var response = await _client.GetAsync($"/api/missions/{mission.Id}/objectives");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<Objective>>();
        result!.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Objetivo via Missão");
    }

    #endregion

    #region Progress Tests

    [Fact]
    public async Task GetProgress_WithValidIds_ReturnsOk()
    {
        var mission = await CreateTestMission();

        var createResponse = await _client.PostAsJsonAsync("/api/objectives",
            new CreateObjectiveRequest { MissionId = mission.Id, Name = "Objetivo" });
        var objective = await createResponse.Content.ReadFromJsonAsync<Objective>();

        var response = await _client.GetAsync($"/api/objectives/progress?ids={objective!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var progress = await response.Content.ReadFromJsonAsync<List<ObjectiveProgressResponse>>();
        progress.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProgress_WithInvalidIds_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/objectives/progress?ids=abc");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
    {
        var unauthenticatedClient = _factory.CreateClient();

        var response = await unauthenticatedClient.GetAsync($"/api/objectives?missionId={Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithTenantMismatch_ReturnsNotFound()
    {
        var leaderId = await GetOrCreateAdminLeader();
        var org1Response = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest { Name = $"obj-org-1-{Guid.NewGuid():N}.com", OwnerId = leaderId });
        var org1 = await org1Response.Content.ReadFromJsonAsync<Organization>();

        var org2Response = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest { Name = $"obj-org-2-{Guid.NewGuid():N}.com", OwnerId = leaderId });
        var org2 = await org2Response.Content.ReadFromJsonAsync<Organization>();

        var missionResponse = await _client.PostAsJsonAsync("/api/missions",
            new CreateMissionRequest
            {
                Name = "Mission Org 2",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(7),
                Status = Bud.Shared.Contracts.MissionStatus.Planned,
                ScopeType = Bud.Shared.Contracts.MissionScopeType.Organization,
                ScopeId = org2!.Id
            });
        var mission = await missionResponse.Content.ReadFromJsonAsync<Mission>();

        var collaborator = await CreateNonOwnerCollaborator(org1!.Id);
        var tenantClient = _factory.CreateTenantClient(org1.Id, collaborator.Email, collaborator.Id);

        var request = new CreateObjectiveRequest
        {
            MissionId = mission!.Id,
            Name = "Objetivo Proibido"
        };

        var response = await tenantClient.PostAsJsonAsync("/api/objectives", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Metric with Objective Tests

    [Fact]
    public async Task CreateMetric_WithObjectiveId_AssociatesCorrectly()
    {
        var mission = await CreateTestMission();

        var objectiveResponse = await _client.PostAsJsonAsync("/api/objectives",
            new CreateObjectiveRequest { MissionId = mission.Id, Name = "Objetivo" });
        var objective = await objectiveResponse.Content.ReadFromJsonAsync<Objective>();

        var metricResponse = await _client.PostAsJsonAsync("/api/metrics",
            new CreateMetricRequest
            {
                MissionId = mission.Id,
                ObjectiveId = objective!.Id,
                Name = "Métrica do Objetivo",
                Type = Bud.Shared.Contracts.MetricType.Qualitative,
                TargetText = "Teste"
            });

        metricResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var metric = await metricResponse.Content.ReadFromJsonAsync<Metric>();
        metric!.ObjectiveId.Should().Be(objective.Id);
    }

    [Fact]
    public async Task GetMetrics_FilteredByObjective_ReturnsCorrectMetrics()
    {
        var mission = await CreateTestMission();

        var objectiveResponse = await _client.PostAsJsonAsync("/api/objectives",
            new CreateObjectiveRequest { MissionId = mission.Id, Name = "Objetivo" });
        var objective = await objectiveResponse.Content.ReadFromJsonAsync<Objective>();

        await _client.PostAsJsonAsync("/api/metrics",
            new CreateMetricRequest
            {
                MissionId = mission.Id,
                ObjectiveId = objective!.Id,
                Name = "Métrica do Objetivo",
                Type = Bud.Shared.Contracts.MetricType.Qualitative,
                TargetText = "T1"
            });

        await _client.PostAsJsonAsync("/api/metrics",
            new CreateMetricRequest
            {
                MissionId = mission.Id,
                Name = "Métrica Direta",
                Type = Bud.Shared.Contracts.MetricType.Qualitative,
                TargetText = "T2"
            });

        var response = await _client.GetAsync($"/api/metrics?missionId={mission.Id}&objectiveId={objective.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<Metric>>();
        result!.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Métrica do Objetivo");
    }

    #endregion

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
}
