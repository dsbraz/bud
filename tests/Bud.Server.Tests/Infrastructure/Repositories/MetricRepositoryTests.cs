using Bud.Server.Infrastructure.Persistence;
using Bud.Server.Domain.Repositories;
using Bud.Server.Tests.Helpers;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bud.Server.Tests.Infrastructure.Repositories;

public sealed class MetricRepositoryTests
{
    private readonly TestTenantProvider _tenantProvider = new() { IsGlobalAdmin = true };

    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, _tenantProvider);
    }

    private static async Task<Goal> CreateTestMission(ApplicationDbContext context)
    {
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        context.Organizations.Add(org);

        var mission = new Goal
        {
            Id = Guid.NewGuid(),
            Name = "Test Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = GoalStatus.Planned,
            OrganizationId = org.Id
        };

        context.Goals.Add(mission);
        await context.SaveChangesAsync();

        return mission;
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenMetricExists_ReturnsMetric()
    {
        using var context = CreateInMemoryContext();
        var repository = new IndicatorRepository(context);
        var mission = await CreateTestMission(context);

        var metric = new Indicator
        {
            Id = Guid.NewGuid(),
            Name = "Test Metric",
            Type = IndicatorType.Qualitative,
            GoalId = mission.Id,
            OrganizationId = mission.OrganizationId
        };
        context.Indicators.Add(metric);
        await context.SaveChangesAsync();

        var result = await repository.GetByIdAsync(metric.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(metric.Id);
        result.Name.Should().Be("Test Metric");
    }

    [Fact]
    public async Task GetByIdAsync_WhenMetricNotFound_ReturnsNull()
    {
        using var context = CreateInMemoryContext();
        var repository = new IndicatorRepository(context);

        var result = await repository.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithCaseInsensitiveSearch_FiltersCorrectly()
    {
        using var context = CreateInMemoryContext();
        var repository = new IndicatorRepository(context);
        var mission = await CreateTestMission(context);

        context.Indicators.AddRange(
            new Indicator
            {
                Id = Guid.NewGuid(),
                Name = "ALPHA Metric",
                Type = IndicatorType.Qualitative,
                GoalId = mission.Id,
                OrganizationId = mission.OrganizationId
            },
            new Indicator
            {
                Id = Guid.NewGuid(),
                Name = "Beta Metric",
                Type = IndicatorType.Qualitative,
                GoalId = mission.Id,
                OrganizationId = mission.OrganizationId
            });
        await context.SaveChangesAsync();

        var result = await repository.GetAllAsync(mission.Id, "alpha", 1, 10);

        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("ALPHA Metric");
    }

    [Fact]
    public async Task GetAllAsync_FiltersByMissionId()
    {
        using var context = CreateInMemoryContext();
        var repository = new IndicatorRepository(context);
        var mission1 = await CreateTestMission(context);
        var mission2 = await CreateTestMission(context);

        context.Indicators.AddRange(
            new Indicator
            {
                Id = Guid.NewGuid(),
                Name = "Metric A",
                Type = IndicatorType.Qualitative,
                GoalId = mission1.Id,
                OrganizationId = mission1.OrganizationId
            },
            new Indicator
            {
                Id = Guid.NewGuid(),
                Name = "Metric B",
                Type = IndicatorType.Qualitative,
                GoalId = mission2.Id,
                OrganizationId = mission2.OrganizationId
            });
        await context.SaveChangesAsync();

        var result = await repository.GetAllAsync(mission1.Id, null, 1, 10);

        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Metric A");
    }

    [Fact]
    public async Task GetAllAsync_FiltersByGoalId()
    {
        using var context = CreateInMemoryContext();
        var repository = new IndicatorRepository(context);
        var mission1 = await CreateTestMission(context);
        var mission2 = await CreateTestMission(context);

        context.Indicators.AddRange(
            new Indicator
            {
                Id = Guid.NewGuid(),
                Name = "In Goal1",
                Type = IndicatorType.Qualitative,
                GoalId = mission1.Id,
                OrganizationId = mission1.OrganizationId
            },
            new Indicator
            {
                Id = Guid.NewGuid(),
                Name = "In Goal2",
                Type = IndicatorType.Qualitative,
                GoalId = mission2.Id,
                OrganizationId = mission2.OrganizationId
            });
        await context.SaveChangesAsync();

        var result = await repository.GetAllAsync(mission1.Id, null, 1, 10);

        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("In Goal1");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedResults()
    {
        using var context = CreateInMemoryContext();
        var repository = new IndicatorRepository(context);
        var mission = await CreateTestMission(context);

        for (int i = 0; i < 5; i++)
        {
            context.Indicators.Add(new Indicator
            {
                Id = Guid.NewGuid(),
                Name = $"Metric {i:D2}",
                Type = IndicatorType.Qualitative,
                GoalId = mission.Id,
                OrganizationId = mission.OrganizationId
            });
        }
        await context.SaveChangesAsync();

        var result = await repository.GetAllAsync(mission.Id, null, 1, 2);

        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    #endregion

    #region GetGoalByIdAsync Tests

    [Fact]
    public async Task GetMissionByIdAsync_WhenExists_ReturnsMission()
    {
        using var context = CreateInMemoryContext();
        var repository = new IndicatorRepository(context);
        var mission = await CreateTestMission(context);

        var result = await repository.GetGoalByIdAsync(mission.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(mission.Id);
    }

    [Fact]
    public async Task GetMissionByIdAsync_WhenNotFound_ReturnsNull()
    {
        using var context = CreateInMemoryContext();
        var repository = new IndicatorRepository(context);

        var result = await repository.GetGoalByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    #endregion

    #region GetGoalByIdAsync Tests

    [Fact]
    public async Task GetObjectiveByIdAsync_WhenExists_ReturnsObjective()
    {
        using var context = CreateInMemoryContext();
        var repository = new IndicatorRepository(context);
        var mission = await CreateTestMission(context);

        var objective = new Goal
        {
            Id = Guid.NewGuid(),
            Name = "Test Objective",
            ParentId = mission.Id,
            OrganizationId = mission.OrganizationId
        };
        context.Goals.Add(objective);
        await context.SaveChangesAsync();

        var result = await repository.GetGoalByIdAsync(objective.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(objective.Id);
    }

    [Fact]
    public async Task GetObjectiveByIdAsync_WhenNotFound_ReturnsNull()
    {
        using var context = CreateInMemoryContext();
        var repository = new IndicatorRepository(context);

        var result = await repository.GetGoalByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    #endregion

    #region AddAsync / RemoveAsync / SaveChangesAsync Tests

    [Fact]
    public async Task AddAsync_PersistsMetric()
    {
        using var context = CreateInMemoryContext();
        var repository = new IndicatorRepository(context);
        var mission = await CreateTestMission(context);

        var metric = Indicator.Create(
            Guid.NewGuid(),
            mission.OrganizationId,
            mission.Id,
            "New Metric",
            IndicatorType.Qualitative);

        await repository.AddAsync(metric);
        await repository.SaveChangesAsync();

        var persisted = await context.Indicators.FindAsync(metric.Id);
        persisted.Should().NotBeNull();
        persisted!.Name.Should().Be("New Metric");
    }

    [Fact]
    public async Task RemoveAsync_DeletesMetric()
    {
        using var context = CreateInMemoryContext();
        var repository = new IndicatorRepository(context);
        var mission = await CreateTestMission(context);

        var metric = new Indicator
        {
            Id = Guid.NewGuid(),
            Name = "To Delete",
            Type = IndicatorType.Qualitative,
            GoalId = mission.Id,
            OrganizationId = mission.OrganizationId
        };
        context.Indicators.Add(metric);
        await context.SaveChangesAsync();

        await repository.RemoveAsync(metric);
        await repository.SaveChangesAsync();

        var persisted = await context.Indicators.FindAsync(metric.Id);
        persisted.Should().BeNull();
    }

    #endregion
}
