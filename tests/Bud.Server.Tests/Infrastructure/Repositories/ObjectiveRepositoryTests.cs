using Bud.Server.Infrastructure.Persistence;
using Bud.Server.Domain.Repositories;
using Bud.Server.Tests.Helpers;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bud.Server.Tests.Infrastructure.Repositories;

public sealed class ObjectiveRepositoryTests
{
    private readonly TestTenantProvider _tenantProvider = new() { IsGlobalAdmin = true };

    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, _tenantProvider);
    }

    private static async Task<Mission> CreateTestMission(ApplicationDbContext context)
    {
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        context.Organizations.Add(org);

        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Test Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = MissionStatus.Planned,
            OrganizationId = org.Id
        };

        context.Missions.Add(mission);
        await context.SaveChangesAsync();

        return mission;
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenObjectiveExists_ReturnsObjective()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new ObjectiveRepository(context);
        var mission = await CreateTestMission(context);

        var objective = new Objective
        {
            Id = Guid.NewGuid(),
            Name = "Test Objective",
            MissionId = mission.Id,
            OrganizationId = mission.OrganizationId
        };
        context.Objectives.Add(objective);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdAsync(objective.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(objective.Id);
        result.Name.Should().Be("Test Objective");
    }

    [Fact]
    public async Task GetByIdAsync_WhenObjectiveNotFound_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new ObjectiveRepository(context);

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByIdForUpdateAsync Tests

    [Fact]
    public async Task GetByIdForUpdateAsync_WhenObjectiveExists_ReturnsTrackedObjective()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new ObjectiveRepository(context);
        var mission = await CreateTestMission(context);

        var objective = new Objective
        {
            Id = Guid.NewGuid(),
            Name = "Tracked Objective",
            MissionId = mission.Id,
            OrganizationId = mission.OrganizationId
        };
        context.Objectives.Add(objective);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdForUpdateAsync(objective.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(objective.Id);
        result.Name.Should().Be("Tracked Objective");
    }

    [Fact]
    public async Task GetByIdForUpdateAsync_WhenObjectiveNotFound_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new ObjectiveRepository(context);

        // Act
        var result = await repository.GetByIdForUpdateAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_FiltersByMissionId()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new ObjectiveRepository(context);
        var mission1 = await CreateTestMission(context);
        var mission2 = await CreateTestMission(context);

        context.Objectives.AddRange(
            new Objective
            {
                Id = Guid.NewGuid(),
                Name = "Objective A",
                MissionId = mission1.Id,
                OrganizationId = mission1.OrganizationId
            },
            new Objective
            {
                Id = Guid.NewGuid(),
                Name = "Objective B",
                MissionId = mission2.Id,
                OrganizationId = mission2.OrganizationId
            });
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync(mission1.Id, 1, 10);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Objective A");
    }

    [Fact]
    public async Task GetAllAsync_WithoutMissionIdFilter_ReturnsAll()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new ObjectiveRepository(context);
        var mission1 = await CreateTestMission(context);
        var mission2 = await CreateTestMission(context);

        context.Objectives.AddRange(
            new Objective
            {
                Id = Guid.NewGuid(),
                Name = "Objective A",
                MissionId = mission1.Id,
                OrganizationId = mission1.OrganizationId
            },
            new Objective
            {
                Id = Guid.NewGuid(),
                Name = "Objective B",
                MissionId = mission2.Id,
                OrganizationId = mission2.OrganizationId
            });
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync(null, 1, 10);

        // Assert
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedResults()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new ObjectiveRepository(context);
        var mission = await CreateTestMission(context);

        for (int i = 0; i < 5; i++)
        {
            context.Objectives.Add(new Objective
            {
                Id = Guid.NewGuid(),
                Name = $"Objective {i:D2}",
                MissionId = mission.Id,
                OrganizationId = mission.OrganizationId
            });
        }
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync(mission.Id, 1, 2);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOrderedByName()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new ObjectiveRepository(context);
        var mission = await CreateTestMission(context);

        context.Objectives.AddRange(
            new Objective
            {
                Id = Guid.NewGuid(),
                Name = "Charlie",
                MissionId = mission.Id,
                OrganizationId = mission.OrganizationId
            },
            new Objective
            {
                Id = Guid.NewGuid(),
                Name = "Alpha",
                MissionId = mission.Id,
                OrganizationId = mission.OrganizationId
            },
            new Objective
            {
                Id = Guid.NewGuid(),
                Name = "Bravo",
                MissionId = mission.Id,
                OrganizationId = mission.OrganizationId
            });
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync(mission.Id, 1, 10);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Name.Should().Be("Alpha");
        result.Items[1].Name.Should().Be("Bravo");
        result.Items[2].Name.Should().Be("Charlie");
    }

    #endregion

    #region AddAsync / RemoveAsync / SaveChangesAsync Tests

    [Fact]
    public async Task AddAsync_PersistsObjective()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new ObjectiveRepository(context);
        var mission = await CreateTestMission(context);

        var objective = Objective.Create(
            Guid.NewGuid(),
            mission.OrganizationId,
            mission.Id,
            "New Objective",
            "A description");

        // Act
        await repository.AddAsync(objective);
        await repository.SaveChangesAsync();

        // Assert
        var persisted = await context.Objectives.FindAsync(objective.Id);
        persisted.Should().NotBeNull();
        persisted!.Name.Should().Be("New Objective");
    }

    [Fact]
    public async Task RemoveAsync_DeletesObjective()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new ObjectiveRepository(context);
        var mission = await CreateTestMission(context);

        var objective = new Objective
        {
            Id = Guid.NewGuid(),
            Name = "To Delete",
            MissionId = mission.Id,
            OrganizationId = mission.OrganizationId
        };
        context.Objectives.Add(objective);
        await context.SaveChangesAsync();

        // Act
        await repository.RemoveAsync(objective);
        await repository.SaveChangesAsync();

        // Assert
        var persisted = await context.Objectives.FindAsync(objective.Id);
        persisted.Should().BeNull();
    }

    #endregion
}
