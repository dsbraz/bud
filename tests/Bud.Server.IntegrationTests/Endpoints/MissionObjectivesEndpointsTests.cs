using System.Net;
using System.Net.Http.Json;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
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

        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "getbud.co", OrganizationId = org.Id };
        dbContext.Workspaces.Add(workspace);

        var adminLeader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Administrador",
            Email = "admin@getbud.co",
            Role = CollaboratorRole.Leader,
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
                Status = MissionStatus.Planned,
                ScopeType = MissionScopeType.Organization,
                ScopeId = org!.Id
            });

        return (await missionResponse.Content.ReadFromJsonAsync<Mission>())!;
    }

    #region Create Tests

    [Fact]
    public async Task Create_WithValidData_ReturnsCreated()
    {
        var mission = await CreateTestMission();

        var request = new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Objetivo Estratégico",
            Description = "Descrição do objetivo"
        };

        var response = await _client.PostAsJsonAsync("/api/mission-objectives", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var objective = await response.Content.ReadFromJsonAsync<MissionObjective>();
        objective.Should().NotBeNull();
        objective!.Name.Should().Be("Objetivo Estratégico");
        objective.Description.Should().Be("Descrição do objetivo");
        objective.MissionId.Should().Be(mission.Id);
        objective.OrganizationId.Should().Be(mission.OrganizationId);
        objective.ParentObjectiveId.Should().BeNull();
    }

    [Fact]
    public async Task Create_WithParentObjective_ReturnsCreated()
    {
        var mission = await CreateTestMission();

        var parentResponse = await _client.PostAsJsonAsync("/api/mission-objectives",
            new CreateMissionObjectiveRequest { MissionId = mission.Id, Name = "Pai" });
        var parent = await parentResponse.Content.ReadFromJsonAsync<MissionObjective>();

        var childResponse = await _client.PostAsJsonAsync("/api/mission-objectives",
            new CreateMissionObjectiveRequest
            {
                MissionId = mission.Id,
                Name = "Filho",
                ParentObjectiveId = parent!.Id
            });

        childResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var child = await childResponse.Content.ReadFromJsonAsync<MissionObjective>();
        child!.ParentObjectiveId.Should().Be(parent.Id);
    }

    [Fact]
    public async Task Create_WithInvalidMission_ReturnsNotFound()
    {
        var request = new CreateMissionObjectiveRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Objetivo"
        };

        var response = await _client.PostAsJsonAsync("/api/mission-objectives", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithInvalidParent_ReturnsNotFound()
    {
        var mission = await CreateTestMission();

        var request = new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Objetivo",
            ParentObjectiveId = Guid.NewGuid()
        };

        var response = await _client.PostAsJsonAsync("/api/mission-objectives", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithParentFromDifferentMission_ReturnsBadRequest()
    {
        var mission1 = await CreateTestMission();
        var mission2 = await CreateTestMission();

        var parentResponse = await _client.PostAsJsonAsync("/api/mission-objectives",
            new CreateMissionObjectiveRequest { MissionId = mission1.Id, Name = "Pai Missão 1" });
        var parent = await parentResponse.Content.ReadFromJsonAsync<MissionObjective>();

        var response = await _client.PostAsJsonAsync("/api/mission-objectives",
            new CreateMissionObjectiveRequest
            {
                MissionId = mission2.Id,
                Name = "Filho Missão 2",
                ParentObjectiveId = parent!.Id
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithEmptyName_ReturnsBadRequest()
    {
        var mission = await CreateTestMission();

        var request = new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = ""
        };

        var response = await _client.PostAsJsonAsync("/api/mission-objectives", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidData_ReturnsOk()
    {
        var mission = await CreateTestMission();

        var createResponse = await _client.PostAsJsonAsync("/api/mission-objectives",
            new CreateMissionObjectiveRequest { MissionId = mission.Id, Name = "Original" });
        var created = await createResponse.Content.ReadFromJsonAsync<MissionObjective>();

        var updateRequest = new UpdateMissionObjectiveRequest
        {
            Name = "Atualizado",
            Description = "Nova descrição"
        };

        var response = await _client.PutAsJsonAsync($"/api/mission-objectives/{created!.Id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<MissionObjective>();
        updated!.Name.Should().Be("Atualizado");
        updated.Description.Should().Be("Nova descrição");
    }

    [Fact]
    public async Task Update_WithNonExistentId_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync($"/api/mission-objectives/{Guid.NewGuid()}",
            new UpdateMissionObjectiveRequest { Name = "X" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithNoChildren_ReturnsNoContent()
    {
        var mission = await CreateTestMission();

        var createResponse = await _client.PostAsJsonAsync("/api/mission-objectives",
            new CreateMissionObjectiveRequest { MissionId = mission.Id, Name = "Objetivo" });
        var created = await createResponse.Content.ReadFromJsonAsync<MissionObjective>();

        var response = await _client.DeleteAsync($"/api/mission-objectives/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_WithChildren_ReturnsBadRequest()
    {
        var mission = await CreateTestMission();

        var parentResponse = await _client.PostAsJsonAsync("/api/mission-objectives",
            new CreateMissionObjectiveRequest { MissionId = mission.Id, Name = "Pai" });
        var parent = await parentResponse.Content.ReadFromJsonAsync<MissionObjective>();

        await _client.PostAsJsonAsync("/api/mission-objectives",
            new CreateMissionObjectiveRequest
            {
                MissionId = mission.Id,
                Name = "Filho",
                ParentObjectiveId = parent!.Id
            });

        var response = await _client.DeleteAsync($"/api/mission-objectives/{parent.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_WithNonExistentId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/api/mission-objectives/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ReturnsOk()
    {
        var mission = await CreateTestMission();

        var createResponse = await _client.PostAsJsonAsync("/api/mission-objectives",
            new CreateMissionObjectiveRequest { MissionId = mission.Id, Name = "Objetivo" });
        var created = await createResponse.Content.ReadFromJsonAsync<MissionObjective>();

        var response = await _client.GetAsync($"/api/mission-objectives/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var objective = await response.Content.ReadFromJsonAsync<MissionObjective>();
        objective!.Name.Should().Be("Objetivo");
    }

    [Fact]
    public async Task GetById_WithNonExistentId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/mission-objectives/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsTopLevelOnly()
    {
        var mission = await CreateTestMission();

        var parentResponse = await _client.PostAsJsonAsync("/api/mission-objectives",
            new CreateMissionObjectiveRequest { MissionId = mission.Id, Name = "Pai" });
        var parent = await parentResponse.Content.ReadFromJsonAsync<MissionObjective>();

        await _client.PostAsJsonAsync("/api/mission-objectives",
            new CreateMissionObjectiveRequest
            {
                MissionId = mission.Id,
                Name = "Filho",
                ParentObjectiveId = parent!.Id
            });

        var response = await _client.GetAsync($"/api/mission-objectives?missionId={mission.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<MissionObjective>>();
        result!.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Pai");
    }

    [Fact]
    public async Task GetAll_WithParentFilter_ReturnsChildren()
    {
        var mission = await CreateTestMission();

        var parentResponse = await _client.PostAsJsonAsync("/api/mission-objectives",
            new CreateMissionObjectiveRequest { MissionId = mission.Id, Name = "Pai" });
        var parent = await parentResponse.Content.ReadFromJsonAsync<MissionObjective>();

        await _client.PostAsJsonAsync("/api/mission-objectives",
            new CreateMissionObjectiveRequest
            {
                MissionId = mission.Id,
                Name = "Filho",
                ParentObjectiveId = parent!.Id
            });

        var response = await _client.GetAsync($"/api/mission-objectives?missionId={mission.Id}&parentObjectiveId={parent.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<MissionObjective>>();
        result!.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Filho");
    }

    [Fact]
    public async Task GetAll_WithInvalidPageSize_ReturnsBadRequest()
    {
        var response = await _client.GetAsync($"/api/mission-objectives?missionId={Guid.NewGuid()}&page=1&pageSize=101");

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

        await _client.PostAsJsonAsync("/api/mission-objectives",
            new CreateMissionObjectiveRequest { MissionId = mission.Id, Name = "Objetivo via Missão" });

        var response = await _client.GetAsync($"/api/missions/{mission.Id}/objectives");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<MissionObjective>>();
        result!.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Objetivo via Missão");
    }

    #endregion

    #region Progress Tests

    [Fact]
    public async Task GetProgress_WithValidIds_ReturnsOk()
    {
        var mission = await CreateTestMission();

        var createResponse = await _client.PostAsJsonAsync("/api/mission-objectives",
            new CreateMissionObjectiveRequest { MissionId = mission.Id, Name = "Objetivo" });
        var objective = await createResponse.Content.ReadFromJsonAsync<MissionObjective>();

        var response = await _client.GetAsync($"/api/mission-objectives/progress?ids={objective!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var progress = await response.Content.ReadFromJsonAsync<List<ObjectiveProgressDto>>();
        progress.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProgress_WithInvalidIds_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/mission-objectives/progress?ids=abc");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
    {
        var unauthenticatedClient = _factory.CreateClient();

        var response = await unauthenticatedClient.GetAsync($"/api/mission-objectives?missionId={Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithTenantMismatch_ReturnsForbidden()
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
                Status = MissionStatus.Planned,
                ScopeType = MissionScopeType.Organization,
                ScopeId = org2!.Id
            });
        var mission = await missionResponse.Content.ReadFromJsonAsync<Mission>();

        var collaborator = await CreateNonOwnerCollaborator(org1!.Id);
        var tenantClient = _factory.CreateTenantClient(org1.Id, collaborator.Email, collaborator.Id);

        var request = new CreateMissionObjectiveRequest
        {
            MissionId = mission!.Id,
            Name = "Objetivo Proibido"
        };

        var response = await tenantClient.PostAsJsonAsync("/api/mission-objectives", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Metric with Objective Tests

    [Fact]
    public async Task CreateMetric_WithObjectiveId_AssociatesCorrectly()
    {
        var mission = await CreateTestMission();

        var objectiveResponse = await _client.PostAsJsonAsync("/api/mission-objectives",
            new CreateMissionObjectiveRequest { MissionId = mission.Id, Name = "Objetivo" });
        var objective = await objectiveResponse.Content.ReadFromJsonAsync<MissionObjective>();

        var metricResponse = await _client.PostAsJsonAsync("/api/mission-metrics",
            new CreateMissionMetricRequest
            {
                MissionId = mission.Id,
                MissionObjectiveId = objective!.Id,
                Name = "Métrica do Objetivo",
                Type = MetricType.Qualitative,
                TargetText = "Teste"
            });

        metricResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var metric = await metricResponse.Content.ReadFromJsonAsync<MissionMetric>();
        metric!.MissionObjectiveId.Should().Be(objective.Id);
    }

    [Fact]
    public async Task GetMetrics_FilteredByObjective_ReturnsCorrectMetrics()
    {
        var mission = await CreateTestMission();

        var objectiveResponse = await _client.PostAsJsonAsync("/api/mission-objectives",
            new CreateMissionObjectiveRequest { MissionId = mission.Id, Name = "Objetivo" });
        var objective = await objectiveResponse.Content.ReadFromJsonAsync<MissionObjective>();

        await _client.PostAsJsonAsync("/api/mission-metrics",
            new CreateMissionMetricRequest
            {
                MissionId = mission.Id,
                MissionObjectiveId = objective!.Id,
                Name = "Métrica do Objetivo",
                Type = MetricType.Qualitative,
                TargetText = "T1"
            });

        await _client.PostAsJsonAsync("/api/mission-metrics",
            new CreateMissionMetricRequest
            {
                MissionId = mission.Id,
                Name = "Métrica Direta",
                Type = MetricType.Qualitative,
                TargetText = "T2"
            });

        var response = await _client.GetAsync($"/api/mission-metrics?missionId={mission.Id}&objectiveId={objective.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<MissionMetric>>();
        result!.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Métrica do Objetivo");
    }

    #endregion

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
}
