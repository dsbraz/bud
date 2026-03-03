using Bud.Server.Infrastructure.Persistence;
using Bud.Server.Tests.Helpers;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bud.Server.Tests.Infrastructure.Repositories;

public sealed class MissionRepositoryTests
{
    private readonly TestTenantProvider _tenantProvider = new() { IsGlobalAdmin = true };

    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, _tenantProvider);
    }

    private static async Task<Organization> CreateTestOrganization(ApplicationDbContext context, string name = "Test Org")
    {
        var org = new Organization { Id = Guid.NewGuid(), Name = name };
        context.Organizations.Add(org);
        await context.SaveChangesAsync();
        return org;
    }

    private static Goal CreateTestMission(
        Guid organizationId,
        string name = "Test Mission",
        GoalStatus status = GoalStatus.Planned)
    {
        return new Goal
        {
            Id = Guid.NewGuid(),
            Name = name,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = status,
            OrganizationId = organizationId
        };
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenMissionExists_ReturnsMission()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new GoalRepository(context);
        var org = await CreateTestOrganization(context);

        var mission = CreateTestMission(org.Id);
        context.Goals.Add(mission);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdAsync(mission.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(mission.Id);
        result.Name.Should().Be("Test Mission");
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissionNotFound_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new GoalRepository(context);

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByIdReadOnlyAsync Tests

    [Fact]
    public async Task GetByIdReadOnlyAsync_WhenExists_ReturnsMission()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new GoalRepository(context);
        var org = await CreateTestOrganization(context);

        var mission = CreateTestMission(org.Id, "ReadOnly Mission");
        context.Goals.Add(mission);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdReadOnlyAsync(mission.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(mission.Id);
        result.Name.Should().Be("ReadOnly Mission");
    }

    [Fact]
    public async Task GetByIdReadOnlyAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new GoalRepository(context);

        // Act
        var result = await repository.GetByIdReadOnlyAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedResults()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new GoalRepository(context);
        var org = await CreateTestOrganization(context);

        for (int i = 0; i < 5; i++)
        {
            context.Goals.Add(CreateTestMission(org.Id, $"Mission {i:D2}"));
        }
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync(null, null, null, 1, 2);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task GetAllAsync_WithCaseInsensitiveSearch_FiltersCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new GoalRepository(context);
        var org = await CreateTestOrganization(context);

        context.Goals.AddRange(
            CreateTestMission(org.Id, "ALPHA Mission"),
            CreateTestMission(org.Id, "Beta Mission"));
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync(null, null, "alpha", 1, 10);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("ALPHA Mission");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOrderedByName()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new GoalRepository(context);
        var org = await CreateTestOrganization(context);

        context.Goals.AddRange(
            CreateTestMission(org.Id, "Zebra"),
            CreateTestMission(org.Id, "Alpha"),
            CreateTestMission(org.Id, "Mango"));
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync(null, null, null, 1, 10);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Name.Should().Be("Alpha");
        result.Items[1].Name.Should().Be("Mango");
        result.Items[2].Name.Should().Be("Zebra");
    }

    #endregion

    #region GetIndicatorsAsync Tests

    [Fact]
    public async Task GetIndicatorsAsync_ReturnsMetricsForMission()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new GoalRepository(context);
        var org = await CreateTestOrganization(context);

        var mission = CreateTestMission(org.Id);
        context.Goals.Add(mission);
        await context.SaveChangesAsync();

        context.Indicators.AddRange(
            new Indicator
            {
                Id = Guid.NewGuid(),
                Name = "Metric A",
                Type = IndicatorType.Qualitative,
                GoalId = mission.Id,
                OrganizationId = org.Id
            },
            new Indicator
            {
                Id = Guid.NewGuid(),
                Name = "Metric B",
                Type = IndicatorType.Quantitative,
                GoalId = mission.Id,
                OrganizationId = org.Id
            });
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetIndicatorsAsync(mission.Id, 1, 10);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(2);
    }

    [Fact]
    public async Task GetIndicatorsAsync_DoesNotReturnMetricsFromOtherMissions()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new GoalRepository(context);
        var org = await CreateTestOrganization(context);

        var mission1 = CreateTestMission(org.Id, "Mission 1");
        var mission2 = CreateTestMission(org.Id, "Mission 2");
        context.Goals.AddRange(mission1, mission2);
        await context.SaveChangesAsync();

        context.Indicators.AddRange(
            new Indicator { Id = Guid.NewGuid(), Name = "Metric M1", Type = IndicatorType.Qualitative, GoalId = mission1.Id, OrganizationId = org.Id },
            new Indicator { Id = Guid.NewGuid(), Name = "Metric M2", Type = IndicatorType.Qualitative, GoalId = mission2.Id, OrganizationId = org.Id });
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetIndicatorsAsync(mission1.Id, 1, 10);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Metric M1");
    }

    [Fact]
    public async Task GetIndicatorsAsync_ReturnsPaginatedResults()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new GoalRepository(context);
        var org = await CreateTestOrganization(context);

        var mission = CreateTestMission(org.Id);
        context.Goals.Add(mission);
        await context.SaveChangesAsync();

        for (int i = 0; i < 5; i++)
        {
            context.Indicators.Add(new Indicator
            {
                Id = Guid.NewGuid(),
                Name = $"Metric {i:D2}",
                Type = IndicatorType.Qualitative,
                GoalId = mission.Id,
                OrganizationId = org.Id
            });
        }
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetIndicatorsAsync(mission.Id, 1, 2);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WhenMissionExists_ReturnsTrue()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new GoalRepository(context);
        var org = await CreateTestOrganization(context);

        var mission = CreateTestMission(org.Id);
        context.Goals.Add(mission);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.ExistsAsync(mission.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenMissionNotFound_ReturnsFalse()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new GoalRepository(context);

        // Act
        var result = await repository.ExistsAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region AddAsync / RemoveAsync / SaveChangesAsync Tests

    [Fact]
    public async Task AddAsync_PersistsMission()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new GoalRepository(context);
        var org = await CreateTestOrganization(context);

        var mission = CreateTestMission(org.Id, "New Mission");

        // Act
        await repository.AddAsync(mission);
        await repository.SaveChangesAsync();

        // Assert
        var persisted = await context.Goals.FindAsync(mission.Id);
        persisted.Should().NotBeNull();
        persisted!.Name.Should().Be("New Mission");
    }

    [Fact]
    public async Task RemoveAsync_DeletesMission()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new GoalRepository(context);
        var org = await CreateTestOrganization(context);

        var mission = CreateTestMission(org.Id, "To Delete");
        context.Goals.Add(mission);
        await context.SaveChangesAsync();

        // Re-fetch tracked entity
        var tracked = await context.Goals.FirstAsync(m => m.Id == mission.Id);

        // Act
        await repository.RemoveAsync(tracked);
        await repository.SaveChangesAsync();

        // Assert
        var persisted = await context.Goals.FindAsync(mission.Id);
        persisted.Should().BeNull();
    }

    #endregion
}
