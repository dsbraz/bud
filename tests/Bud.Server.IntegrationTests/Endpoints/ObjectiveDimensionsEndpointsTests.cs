using System.Net;
using System.Net.Http.Json;
using Bud.Server.Data;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bud.Server.IntegrationTests.Endpoints;

public class ObjectiveDimensionsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ObjectiveDimensionsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateGlobalAdminClient();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var orgId = db.Organizations.IgnoreQueryFilters()
            .Where(o => o.Name == "getbud.co")
            .Select(o => o.Id)
            .First();
        _client.DefaultRequestHeaders.Add("X-Tenant-Id", orgId.ToString());
    }

    private async Task<ObjectiveDimension> CreateDimension(string name = "Clientes")
    {
        var response = await _client.PostAsJsonAsync("/api/objective-dimensions", new CreateObjectiveDimensionRequest
        {
            Name = $"{name}-{Guid.NewGuid():N}"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ObjectiveDimension>())!;
    }

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreated()
    {
        var response = await _client.PostAsJsonAsync("/api/objective-dimensions", new CreateObjectiveDimensionRequest
        {
            Name = $"Clientes-{Guid.NewGuid():N}"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<ObjectiveDimension>();
        created.Should().NotBeNull();
        created!.Name.Should().StartWith("Clientes-");
    }

    [Fact]
    public async Task GetAll_ReturnsPagedResult()
    {
        _ = await CreateDimension("Processos");

        var response = await _client.GetAsync("/api/objective-dimensions?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ObjectiveDimension>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Update_WithValidRequest_ReturnsOk()
    {
        var created = await CreateDimension("Pessoas");

        var response = await _client.PutAsJsonAsync($"/api/objective-dimensions/{created.Id}", new UpdateObjectiveDimensionRequest
        {
            Name = "Pessoas Atualizado"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ObjectiveDimension>();
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Pessoas Atualizado");
    }

    [Fact]
    public async Task Delete_WhenInUse_ReturnsConflict()
    {
        var dimension = await CreateDimension("Financeiro");

        var mission = await CreateMission();
        var objectiveResponse = await _client.PostAsJsonAsync("/api/mission-objectives", new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Objetivo com dimensão",
            ObjectiveDimensionId = dimension.Id
        });
        objectiveResponse.EnsureSuccessStatusCode();

        var response = await _client.DeleteAsync($"/api/objective-dimensions/{dimension.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_WhenUsedByTemplateObjectives_ReturnsConflict()
    {
        var dimension = await CreateDimension("Template Dim");

        // Create template with objective referencing the dimension via DbContext
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var org = await dbContext.Organizations.IgnoreQueryFilters().FirstAsync(o => o.Name == "getbud.co");

        var template = new MissionTemplate
        {
            Id = Guid.NewGuid(),
            Name = $"Template-{Guid.NewGuid():N}",
            OrganizationId = org.Id,
            IsDefault = false,
            IsActive = true
        };
        dbContext.MissionTemplates.Add(template);
        await dbContext.SaveChangesAsync();

        var templateObjective = new MissionTemplateObjective
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            MissionTemplateId = template.Id,
            Name = "Objetivo Template",
            OrderIndex = 0,
            ObjectiveDimensionId = dimension.Id
        };
        dbContext.MissionTemplateObjectives.Add(templateObjective);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/objective-dimensions/{dimension.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private async Task<Mission> CreateMission()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var leader = await dbContext.Collaborators.IgnoreQueryFilters().FirstAsync(c => c.Email == "admin@getbud.co");
        var org = await dbContext.Organizations.IgnoreQueryFilters().FirstAsync(o => o.OwnerId == leader.Id);

        var missionResponse = await _client.PostAsJsonAsync("/api/missions",
            new CreateMissionRequest
            {
                Name = "Missão para dimensão",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(7),
                Status = MissionStatus.Planned,
                ScopeType = MissionScopeType.Organization,
                ScopeId = org.Id
            });

        missionResponse.EnsureSuccessStatusCode();
        return (await missionResponse.Content.ReadFromJsonAsync<Mission>())!;
    }
}
