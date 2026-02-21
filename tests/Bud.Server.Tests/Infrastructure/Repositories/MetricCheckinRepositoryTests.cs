using Bud.Server.Infrastructure.Persistence;
using Bud.Server.Domain.Repositories;
using Bud.Server.Tests.Helpers;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bud.Server.Tests.Infrastructure.Repositories;

public sealed class MetricCheckinRepositoryTests
{
    private readonly TestTenantProvider _tenantProvider = new() { IsGlobalAdmin = true };

    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, _tenantProvider);
    }

    private static async Task<(Organization org, Mission mission)> CreateTestMission(
        ApplicationDbContext context,
        MissionStatus status = MissionStatus.Active)
    {
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        context.Organizations.Add(org);

        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Test Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = status,
            OrganizationId = org.Id
        };

        context.Missions.Add(mission);
        await context.SaveChangesAsync();

        return (org, mission);
    }

    private static async Task<MissionMetric> CreateTestMetric(
        ApplicationDbContext context,
        Guid missionId,
        Guid organizationId,
        MetricType type,
        string name = "Test Metric")
    {
        var metric = new MissionMetric
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            MissionId = missionId,
            Name = name,
            Type = type,
            TargetText = type == MetricType.Qualitative ? "Target text" : null,
            QuantitativeType = type == MetricType.Quantitative ? QuantitativeMetricType.KeepAbove : null,
            MinValue = type == MetricType.Quantitative ? 10m : null,
            Unit = type == MetricType.Quantitative ? MetricUnit.Integer : null
        };

        context.MissionMetrics.Add(metric);
        await context.SaveChangesAsync();

        return metric;
    }

    private static async Task<Collaborator> CreateTestCollaborator(ApplicationDbContext context, Guid organizationId)
    {
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Test Collaborator",
            Email = "test@example.com",
            OrganizationId = organizationId
        };

        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        return collaborator;
    }

    private static MetricCheckin CreateCheckin(
        MissionMetric metric,
        Guid collaboratorId,
        decimal? value = null,
        string? text = null,
        DateTime? checkinDate = null,
        string? note = null,
        int confidenceLevel = 3)
    {
        return metric.CreateCheckin(
            Guid.NewGuid(),
            collaboratorId,
            value,
            text,
            checkinDate ?? DateTime.UtcNow,
            note,
            confidenceLevel);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsCheckin()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repo = new MetricCheckinRepository(context);
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id, MetricType.Quantitative);
        var collaborator = await CreateTestCollaborator(context, org.Id);
        var checkin = CreateCheckin(metric, collaborator.Id, value: 42.5m, note: "Weekly check-in", confidenceLevel: 5);
        context.MetricCheckins.Add(checkin);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetByIdAsync(checkin.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(checkin.Id);
        result.MissionMetricId.Should().Be(metric.Id);
        result.CollaboratorId.Should().Be(collaborator.Id);
        result.Value.Should().Be(42.5m);
        result.Note.Should().Be("Weekly check-in");
        result.ConfidenceLevel.Should().Be(5);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repo = new MetricCheckinRepository(context);

        // Act
        var result = await repo.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithMissionMetricIdFilter_ReturnsOnlyMatchingCheckins()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repo = new MetricCheckinRepository(context);
        var (org, mission) = await CreateTestMission(context);
        var metric1 = await CreateTestMetric(context, mission.Id, org.Id, MetricType.Quantitative, "Metric 1");
        var metric2 = await CreateTestMetric(context, mission.Id, org.Id, MetricType.Quantitative, "Metric 2");
        var collaborator = await CreateTestCollaborator(context, org.Id);

        context.MetricCheckins.Add(CreateCheckin(metric1, collaborator.Id, value: 10m));
        context.MetricCheckins.Add(CreateCheckin(metric1, collaborator.Id, value: 20m));
        context.MetricCheckins.Add(CreateCheckin(metric2, collaborator.Id, value: 50m));
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetAllAsync(missionMetricId: metric1.Id, missionId: null, page: 1, pageSize: 10);

        // Assert
        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(c => c.MissionMetricId.Should().Be(metric1.Id));
    }

    [Fact]
    public async Task GetAllAsync_WithMissionIdFilter_ReturnsOnlyMatchingCheckins()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repo = new MetricCheckinRepository(context);
        var (org, mission1) = await CreateTestMission(context);

        var mission2 = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Another Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = MissionStatus.Planned,
            OrganizationId = org.Id
        };
        context.Missions.Add(mission2);
        await context.SaveChangesAsync();

        var metric1 = await CreateTestMetric(context, mission1.Id, org.Id, MetricType.Quantitative, "Metric M1");
        var metric2 = await CreateTestMetric(context, mission2.Id, org.Id, MetricType.Quantitative, "Metric M2");
        var collaborator = await CreateTestCollaborator(context, org.Id);

        context.MetricCheckins.Add(CreateCheckin(metric1, collaborator.Id, value: 10m));
        context.MetricCheckins.Add(CreateCheckin(metric2, collaborator.Id, value: 20m));
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetAllAsync(missionMetricId: null, missionId: mission1.Id, page: 1, pageSize: 10);

        // Assert
        result.Total.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].MissionMetricId.Should().Be(metric1.Id);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOrderedByCheckinDateDescending()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repo = new MetricCheckinRepository(context);
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id, MetricType.Quantitative);
        var collaborator = await CreateTestCollaborator(context, org.Id);

        var date1 = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var date2 = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var date3 = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        context.MetricCheckins.Add(CreateCheckin(metric, collaborator.Id, value: 10m, checkinDate: date2));
        context.MetricCheckins.Add(CreateCheckin(metric, collaborator.Id, value: 30m, checkinDate: date1));
        context.MetricCheckins.Add(CreateCheckin(metric, collaborator.Id, value: 20m, checkinDate: date3));
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetAllAsync(missionMetricId: metric.Id, missionId: null, page: 1, pageSize: 10);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].CheckinDate.Should().Be(date3);
        result.Items[1].CheckinDate.Should().Be(date2);
        result.Items[2].CheckinDate.Should().Be(date1);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedResults()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repo = new MetricCheckinRepository(context);
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id, MetricType.Quantitative);
        var collaborator = await CreateTestCollaborator(context, org.Id);

        for (var i = 0; i < 5; i++)
        {
            context.MetricCheckins.Add(CreateCheckin(metric, collaborator.Id, value: i * 10m, checkinDate: DateTime.UtcNow.AddDays(i)));
        }

        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetAllAsync(missionMetricId: null, missionId: null, page: 1, pageSize: 2);

        // Assert
        result.Total.Should().Be(5);
        result.Items.Should().HaveCount(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    #endregion

    #region GetMetricWithMissionAsync Tests

    [Fact]
    public async Task GetMetricWithMissionAsync_WhenExists_ReturnsMetricWithMission()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repo = new MetricCheckinRepository(context);
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id, MetricType.Quantitative);

        // Act
        var result = await repo.GetMetricWithMissionAsync(metric.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(metric.Id);
        result.Mission.Should().NotBeNull();
        result.Mission.Id.Should().Be(mission.Id);
    }

    [Fact]
    public async Task GetMetricWithMissionAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repo = new MetricCheckinRepository(context);

        // Act
        var result = await repo.GetMetricWithMissionAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetMetricByIdAsync Tests

    [Fact]
    public async Task GetMetricByIdAsync_WhenExists_ReturnsMetric()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repo = new MetricCheckinRepository(context);
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id, MetricType.Quantitative);

        // Act
        var result = await repo.GetMetricByIdAsync(metric.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(metric.Id);
    }

    [Fact]
    public async Task GetMetricByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repo = new MetricCheckinRepository(context);

        // Act
        var result = await repo.GetMetricByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region AddAsync and RemoveAsync Tests

    [Fact]
    public async Task AddAsync_PersistsCheckin()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repo = new MetricCheckinRepository(context);
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id, MetricType.Quantitative);
        var collaborator = await CreateTestCollaborator(context, org.Id);
        var checkin = CreateCheckin(metric, collaborator.Id, value: 42.5m);

        // Act
        await repo.AddAsync(checkin);
        await repo.SaveChangesAsync();

        // Assert
        var found = await context.MetricCheckins.AsNoTracking().FirstOrDefaultAsync(c => c.Id == checkin.Id);
        found.Should().NotBeNull();
        found!.Value.Should().Be(42.5m);
    }

    [Fact]
    public async Task RemoveAsync_DeletesCheckin()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repo = new MetricCheckinRepository(context);
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id, MetricType.Quantitative);
        var collaborator = await CreateTestCollaborator(context, org.Id);
        var checkin = CreateCheckin(metric, collaborator.Id, value: 10m);
        context.MetricCheckins.Add(checkin);
        await context.SaveChangesAsync();

        // Re-fetch to get tracked entity
        var tracked = await context.MetricCheckins.FirstAsync(c => c.Id == checkin.Id);

        // Act
        await repo.RemoveAsync(tracked);
        await repo.SaveChangesAsync();

        // Assert
        var found = await context.MetricCheckins.AsNoTracking().FirstOrDefaultAsync(c => c.Id == checkin.Id);
        found.Should().BeNull();
    }

    #endregion
}
