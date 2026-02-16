using Bud.Server.Data;
using Bud.Server.MultiTenancy;
using Bud.Server.Services;
using Bud.Server.Tests.Helpers;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bud.Server.Tests.Services;

public sealed class MissionObjectiveServiceTests
{
    private readonly ITenantProvider _tenantProvider = new TestTenantProvider { IsGlobalAdmin = true };

    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options, _tenantProvider);
    }

    private static async Task<Mission> CreateTestMission(ApplicationDbContext context)
    {
        var organization = Organization.Create(Guid.NewGuid(), "Test Org", Guid.NewGuid());
        context.Organizations.Add(organization);

        var mission = Mission.Create(
            Guid.NewGuid(), organization.Id, "Test Mission", null,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(30), MissionStatus.Active);
        context.Missions.Add(mission);

        await context.SaveChangesAsync();
        return mission;
    }

    private static async Task<ObjectiveDimension> CreateTestObjectiveDimension(ApplicationDbContext context, Guid organizationId, string name = "Clientes")
    {
        var dimension = ObjectiveDimension.Create(Guid.NewGuid(), organizationId, name);
        context.ObjectiveDimensions.Add(dimension);
        await context.SaveChangesAsync();
        return dimension;
    }

    [Fact]
    public async Task CreateObjective_WithValidMission_CreatesSuccessfully()
    {
        using var context = CreateInMemoryContext();
        var service = new MissionObjectiveService(context);
        var mission = await CreateTestMission(context);

        var request = new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Objetivo 1",
            Description = "Descrição do objetivo"
        };

        var result = await service.CreateAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Objetivo 1");
        result.Value.Description.Should().Be("Descrição do objetivo");
        result.Value.OrganizationId.Should().Be(mission.OrganizationId);
        result.Value.MissionId.Should().Be(mission.Id);
    }

    [Fact]
    public async Task CreateObjective_WithObjectiveDimensionId_PersistsValue()
    {
        using var context = CreateInMemoryContext();
        var service = new MissionObjectiveService(context);
        var mission = await CreateTestMission(context);
        var dimension = await CreateTestObjectiveDimension(context, mission.OrganizationId);

        var request = new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Objetivo 1",
            ObjectiveDimensionId = dimension.Id
        };

        var result = await service.CreateAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ObjectiveDimensionId.Should().Be(dimension.Id);
    }

    [Fact]
    public async Task CreateObjective_WithInvalidMission_ReturnsNotFound()
    {
        using var context = CreateInMemoryContext();
        var service = new MissionObjectiveService(context);

        var request = new CreateMissionObjectiveRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Objetivo"
        };

        var result = await service.CreateAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Contain("Missão não encontrada");
    }

    [Fact]
    public async Task UpdateObjective_WithValidData_UpdatesSuccessfully()
    {
        using var context = CreateInMemoryContext();
        var service = new MissionObjectiveService(context);
        var mission = await CreateTestMission(context);

        var createResult = await service.CreateAsync(new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Nome Original"
        });

        var result = await service.UpdateAsync(createResult.Value!.Id, new UpdateMissionObjectiveRequest
        {
            Name = "Nome Atualizado",
            Description = "Nova descrição"
        });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Nome Atualizado");
        result.Value.Description.Should().Be("Nova descrição");
    }

    [Fact]
    public async Task UpdateObjective_WithObjectiveDimensionId_UpdatesValue()
    {
        using var context = CreateInMemoryContext();
        var service = new MissionObjectiveService(context);
        var mission = await CreateTestMission(context);
        var dimension1 = await CreateTestObjectiveDimension(context, mission.OrganizationId, "Financeiro");
        var dimension2 = await CreateTestObjectiveDimension(context, mission.OrganizationId, "Clientes");

        var createResult = await service.CreateAsync(new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Nome Original",
            ObjectiveDimensionId = dimension1.Id
        });

        var result = await service.UpdateAsync(createResult.Value!.Id, new UpdateMissionObjectiveRequest
        {
            Name = "Nome Atualizado",
            ObjectiveDimensionId = dimension2.Id
        });

        result.IsSuccess.Should().BeTrue();
        result.Value!.ObjectiveDimensionId.Should().Be(dimension2.Id);
    }

    [Fact]
    public async Task CreateObjective_WithObjectiveDimensionFromAnotherOrganization_ReturnsValidationError()
    {
        using var context = CreateInMemoryContext();
        var service = new MissionObjectiveService(context);
        var mission = await CreateTestMission(context);

        var otherOrganization = Organization.Create(Guid.NewGuid(), "Outra Org", Guid.NewGuid());
        context.Organizations.Add(otherOrganization);
        await context.SaveChangesAsync();
        var otherDimension = await CreateTestObjectiveDimension(context, otherOrganization.Id);

        var result = await service.CreateAsync(new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Objetivo",
            ObjectiveDimensionId = otherDimension.Id
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Contain("Dimensão do objetivo");
    }

    [Fact]
    public async Task UpdateObjective_WithInvalidId_ReturnsNotFound()
    {
        using var context = CreateInMemoryContext();
        var service = new MissionObjectiveService(context);

        var result = await service.UpdateAsync(Guid.NewGuid(), new UpdateMissionObjectiveRequest
        {
            Name = "Nome"
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
    }

    [Fact]
    public async Task DeleteObjective_DeletesSuccessfully()
    {
        using var context = CreateInMemoryContext();
        var service = new MissionObjectiveService(context);
        var mission = await CreateTestMission(context);

        var createResult = await service.CreateAsync(new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Objetivo"
        });

        var result = await service.DeleteAsync(createResult.Value!.Id);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteObjective_WithInvalidId_ReturnsNotFound()
    {
        using var context = CreateInMemoryContext();
        var service = new MissionObjectiveService(context);

        var result = await service.DeleteAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsObjective()
    {
        using var context = CreateInMemoryContext();
        var service = new MissionObjectiveService(context);
        var mission = await CreateTestMission(context);

        var createResult = await service.CreateAsync(new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Objetivo"
        });

        var result = await service.GetByIdAsync(createResult.Value!.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Objetivo");
    }

    [Fact]
    public async Task GetByMissionAsync_ReturnsAllObjectivesFromMission()
    {
        using var context = CreateInMemoryContext();
        var service = new MissionObjectiveService(context);
        var mission = await CreateTestMission(context);
        var otherMission = await CreateTestMission(context);

        await service.CreateAsync(new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Objetivo A"
        });

        await service.CreateAsync(new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Objetivo B"
        });

        await service.CreateAsync(new CreateMissionObjectiveRequest
        {
            MissionId = otherMission.Id,
            Name = "Objetivo Outra Missão"
        });

        var result = await service.GetByMissionAsync(mission.Id, 1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.Items.Select(i => i.Name).Should().BeEquivalentTo(["Objetivo A", "Objetivo B"]);
    }
}
