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
        var orgId = Guid.NewGuid();
        var organization = Organization.Create(Guid.NewGuid(), "Test Org", Guid.NewGuid());
        context.Organizations.Add(organization);

        var mission = Mission.Create(
            Guid.NewGuid(), organization.Id, "Test Mission", null,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(30), MissionStatus.Active);
        context.Missions.Add(mission);

        await context.SaveChangesAsync();
        return mission;
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
        result.Value.ParentObjectiveId.Should().BeNull();
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
    public async Task CreateObjective_WithValidParent_CreatesSubObjective()
    {
        using var context = CreateInMemoryContext();
        var service = new MissionObjectiveService(context);
        var mission = await CreateTestMission(context);

        var parentResult = await service.CreateAsync(new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Objetivo Pai"
        });

        var childResult = await service.CreateAsync(new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Sub-objetivo",
            ParentObjectiveId = parentResult.Value!.Id
        });

        childResult.IsSuccess.Should().BeTrue();
        childResult.Value!.ParentObjectiveId.Should().Be(parentResult.Value!.Id);
    }

    [Fact]
    public async Task CreateObjective_WithInvalidParent_ReturnsNotFound()
    {
        using var context = CreateInMemoryContext();
        var service = new MissionObjectiveService(context);
        var mission = await CreateTestMission(context);

        var result = await service.CreateAsync(new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Sub-objetivo",
            ParentObjectiveId = Guid.NewGuid()
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Contain("Objetivo pai não encontrado");
    }

    [Fact]
    public async Task CreateObjective_WithParentFromDifferentMission_ReturnsValidationError()
    {
        using var context = CreateInMemoryContext();
        var service = new MissionObjectiveService(context);
        var mission1 = await CreateTestMission(context);
        var mission2 = await CreateTestMission(context);

        var parentResult = await service.CreateAsync(new CreateMissionObjectiveRequest
        {
            MissionId = mission1.Id,
            Name = "Objetivo Missão 1"
        });

        var result = await service.CreateAsync(new CreateMissionObjectiveRequest
        {
            MissionId = mission2.Id,
            Name = "Objetivo Missão 2",
            ParentObjectiveId = parentResult.Value!.Id
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Contain("mesma missão");
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
    public async Task DeleteObjective_WithNoChildren_DeletesSuccessfully()
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
    public async Task DeleteObjective_WithChildren_ReturnsValidationError()
    {
        using var context = CreateInMemoryContext();
        var service = new MissionObjectiveService(context);
        var mission = await CreateTestMission(context);

        var parentResult = await service.CreateAsync(new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Pai"
        });

        await service.CreateAsync(new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Filho",
            ParentObjectiveId = parentResult.Value!.Id
        });

        var result = await service.DeleteAsync(parentResult.Value!.Id);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Contain("sub-objetivos");
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
    public async Task GetByMissionAsync_ReturnsTopLevelOnly_WhenNoParent()
    {
        using var context = CreateInMemoryContext();
        var service = new MissionObjectiveService(context);
        var mission = await CreateTestMission(context);

        var parentResult = await service.CreateAsync(new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Pai"
        });

        await service.CreateAsync(new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Filho",
            ParentObjectiveId = parentResult.Value!.Id
        });

        var result = await service.GetByMissionAsync(mission.Id, null, 1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].Name.Should().Be("Pai");
    }

    [Fact]
    public async Task GetByMissionAsync_ReturnsChildren_WhenParentSpecified()
    {
        using var context = CreateInMemoryContext();
        var service = new MissionObjectiveService(context);
        var mission = await CreateTestMission(context);

        var parentResult = await service.CreateAsync(new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Pai"
        });

        await service.CreateAsync(new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Filho",
            ParentObjectiveId = parentResult.Value!.Id
        });

        var result = await service.GetByMissionAsync(mission.Id, parentResult.Value!.Id, 1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].Name.Should().Be("Filho");
    }
}
