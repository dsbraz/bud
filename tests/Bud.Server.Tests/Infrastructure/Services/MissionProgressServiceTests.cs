using Bud.Server.Infrastructure.Persistence;
using Bud.Server.Infrastructure.Services;
using Bud.Server.Tests.Helpers;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bud.Server.Tests.Infrastructure.Services;

public class MissionProgressServiceTests
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
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        context.Organizations.Add(org);

        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Test Mission",
            StartDate = startDate ?? DateTime.UtcNow.AddDays(-15),
            EndDate = endDate ?? DateTime.UtcNow.AddDays(15),
            Status = MissionStatus.Active,
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
        MetricType type = MetricType.Quantitative,
        QuantitativeMetricType? quantitativeType = QuantitativeMetricType.Achieve,
        decimal? minValue = null,
        decimal? maxValue = 100m)
    {
        var metric = new MissionMetric
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            MissionId = missionId,
            Name = "Test Metric",
            Type = type,
            QuantitativeType = type == MetricType.Quantitative ? quantitativeType : null,
            MinValue = type == MetricType.Quantitative ? minValue : null,
            MaxValue = type == MetricType.Quantitative ? maxValue : null,
            Unit = type == MetricType.Quantitative ? MetricUnit.Integer : null,
            TargetText = type == MetricType.Qualitative ? "Target text" : null
        };

        context.MissionMetrics.Add(metric);
        await context.SaveChangesAsync();

        return metric;
    }

    private static async Task<MetricCheckin> CreateTestCheckin(
        ApplicationDbContext context,
        Guid metricId,
        Guid organizationId,
        decimal? value = null,
        string? text = null,
        int confidenceLevel = 3,
        DateTime? checkinDate = null,
        string? collaboratorName = null,
        Guid? collaboratorId = null)
    {
        var collaborator = new Collaborator
        {
            Id = collaboratorId ?? Guid.NewGuid(),
            FullName = collaboratorName ?? "Test User",
            Email = $"test-{Guid.NewGuid():N}@example.com",
            OrganizationId = organizationId
        };
        context.Collaborators.Add(collaborator);

        var checkin = new MetricCheckin
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            MissionMetricId = metricId,
            CollaboratorId = collaborator.Id,
            Value = value,
            Text = text,
            CheckinDate = checkinDate ?? DateTime.UtcNow,
            ConfidenceLevel = confidenceLevel
        };

        context.MetricCheckins.Add(checkin);
        await context.SaveChangesAsync();

        return checkin;
    }

    [Fact]
    public async Task GetProgressAsync_EmptyList_ReturnsEmptyResult()
    {
        await using var context = CreateInMemoryContext();
        var service = new MissionProgressService(context);

        var result = await service.GetProgressAsync([]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProgressAsync_MissionWithNoMetrics_ReturnsZeroProgress()
    {
        await using var context = CreateInMemoryContext();
        var (_, mission) = await CreateTestMission(context);
        var service = new MissionProgressService(context);

        var result = await service.GetProgressAsync([mission.Id]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        var progress = result.Value![0];
        progress.MissionId.Should().Be(mission.Id);
        progress.OverallProgress.Should().Be(0m);
        progress.TotalMetrics.Should().Be(0);
        progress.MetricsWithCheckins.Should().Be(0);
    }

    [Fact]
    public async Task GetProgressAsync_MetricWithNoCheckins_ReturnsZeroProgress()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        await CreateTestMetric(context, mission.Id, org.Id);
        var service = new MissionProgressService(context);

        var result = await service.GetProgressAsync([mission.Id]);

        result.IsSuccess.Should().BeTrue();
        var progress = result.Value![0];
        progress.OverallProgress.Should().Be(0m);
        progress.TotalMetrics.Should().Be(1);
        progress.MetricsWithCheckins.Should().Be(0);
    }

    [Fact]
    public async Task GetProgressAsync_AchieveMetric_CalculatesCorrectly()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.Achieve, maxValue: 100m);
        await CreateTestCheckin(context, metric.Id, org.Id, value: 60m);
        var service = new MissionProgressService(context);

        var result = await service.GetProgressAsync([mission.Id]);

        var progress = result.Value![0];
        progress.OverallProgress.Should().Be(60m);
        progress.MetricsWithCheckins.Should().Be(1);
    }

    [Fact]
    public async Task GetProgressAsync_AchieveMetric_ClampsAt100()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.Achieve, maxValue: 50m);
        await CreateTestCheckin(context, metric.Id, org.Id, value: 75m);
        var service = new MissionProgressService(context);

        var result = await service.GetProgressAsync([mission.Id]);

        result.Value![0].OverallProgress.Should().Be(100m);
    }

    [Fact]
    public async Task GetProgressAsync_ReduceMetric_WithTwoCheckins_CalculatesCorrectly()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.Reduce, maxValue: 10m);

        // First check-in (baseline): 50
        await CreateTestCheckin(context, metric.Id, org.Id, value: 50m,
            checkinDate: DateTime.UtcNow.AddDays(-10));
        // Latest check-in: 30 (reduced from 50 toward target 10)
        await CreateTestCheckin(context, metric.Id, org.Id, value: 30m,
            checkinDate: DateTime.UtcNow);

        var service = new MissionProgressService(context);
        var result = await service.GetProgressAsync([mission.Id]);

        // progress = (50 - 30) / (50 - 10) * 100 = 20/40 * 100 = 50%
        result.Value![0].OverallProgress.Should().Be(50m);
    }

    [Fact]
    public async Task GetProgressAsync_ReduceMetric_AlreadyAchieved_Returns100()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.Reduce, maxValue: 10m);

        await CreateTestCheckin(context, metric.Id, org.Id, value: 50m,
            checkinDate: DateTime.UtcNow.AddDays(-5));
        await CreateTestCheckin(context, metric.Id, org.Id, value: 5m,
            checkinDate: DateTime.UtcNow);

        var service = new MissionProgressService(context);
        var result = await service.GetProgressAsync([mission.Id]);

        result.Value![0].OverallProgress.Should().Be(100m);
    }

    [Fact]
    public async Task GetProgressAsync_KeepAboveMetric_AboveThreshold_Returns100()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.KeepAbove, minValue: 70m, maxValue: null);
        await CreateTestCheckin(context, metric.Id, org.Id, value: 85m);
        var service = new MissionProgressService(context);

        var result = await service.GetProgressAsync([mission.Id]);

        result.Value![0].OverallProgress.Should().Be(100m);
    }

    [Fact]
    public async Task GetProgressAsync_KeepAboveMetric_BelowThreshold_ReturnsProportional()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.KeepAbove, minValue: 70m, maxValue: null);
        await CreateTestCheckin(context, metric.Id, org.Id, value: 60m);
        var service = new MissionProgressService(context);

        var result = await service.GetProgressAsync([mission.Id]);

        // 60/70 * 100 = 85.7%
        result.Value![0].OverallProgress.Should().Be(85.7m);
    }

    [Fact]
    public async Task GetProgressAsync_KeepBelowMetric_BelowThreshold_Returns100()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.KeepBelow, minValue: null, maxValue: 50m);
        await CreateTestCheckin(context, metric.Id, org.Id, value: 30m);
        var service = new MissionProgressService(context);

        var result = await service.GetProgressAsync([mission.Id]);

        result.Value![0].OverallProgress.Should().Be(100m);
    }

    [Fact]
    public async Task GetProgressAsync_KeepBelowMetric_AboveThreshold_ReturnsProportional()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.KeepBelow, minValue: null, maxValue: 50m);
        await CreateTestCheckin(context, metric.Id, org.Id, value: 60m);
        var service = new MissionProgressService(context);

        var result = await service.GetProgressAsync([mission.Id]);

        // 50/60 * 100 = 83.3%
        result.Value![0].OverallProgress.Should().Be(83.3m);
    }

    [Fact]
    public async Task GetProgressAsync_KeepBetweenMetric_InRange_Returns100()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.KeepBetween, minValue: 80m, maxValue: 95m);
        await CreateTestCheckin(context, metric.Id, org.Id, value: 85m);
        var service = new MissionProgressService(context);

        var result = await service.GetProgressAsync([mission.Id]);

        result.Value![0].OverallProgress.Should().Be(100m);
    }

    [Fact]
    public async Task GetProgressAsync_KeepBetweenMetric_BelowRange_ReturnsProportional()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.KeepBetween, minValue: 80m, maxValue: 95m);
        await CreateTestCheckin(context, metric.Id, org.Id, value: 70m);
        var service = new MissionProgressService(context);

        var result = await service.GetProgressAsync([mission.Id]);

        // 70/80 * 100 = 87.5%
        result.Value![0].OverallProgress.Should().Be(87.5m);
    }

    [Fact]
    public async Task GetProgressAsync_QualitativeMetric_UsesConfidenceAsProxy()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            type: MetricType.Qualitative);
        await CreateTestCheckin(context, metric.Id, org.Id, text: "Some progress", confidenceLevel: 4);
        var service = new MissionProgressService(context);

        var result = await service.GetProgressAsync([mission.Id]);

        // confidence 4/5 * 100 = 80%
        result.Value![0].OverallProgress.Should().Be(80m);
    }

    [Fact]
    public async Task GetProgressAsync_MultipleMetrics_AveragesProgress()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);

        // Metric 1: Achieve 100, current = 60 → 60%
        var metric1 = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.Achieve, maxValue: 100m);
        await CreateTestCheckin(context, metric1.Id, org.Id, value: 60m, confidenceLevel: 4);

        // Metric 2: KeepAbove 70, current = 80 → 100%
        var metric2 = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.KeepAbove, minValue: 70m, maxValue: null);
        await CreateTestCheckin(context, metric2.Id, org.Id, value: 80m, confidenceLevel: 3);

        var service = new MissionProgressService(context);
        var result = await service.GetProgressAsync([mission.Id]);

        var progress = result.Value![0];
        // Average: (60 + 100) / 2 = 80%
        progress.OverallProgress.Should().Be(80m);
        progress.TotalMetrics.Should().Be(2);
        progress.MetricsWithCheckins.Should().Be(2);
        // Average confidence: (4 + 3) / 2 = 3.5
        progress.AverageConfidence.Should().Be(3.5m);
    }

    [Fact]
    public async Task GetProgressAsync_UsesLatestCheckin()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.Achieve, maxValue: 100m);

        // Older check-in with lower value
        await CreateTestCheckin(context, metric.Id, org.Id, value: 20m,
            checkinDate: DateTime.UtcNow.AddDays(-5));
        // Latest check-in with higher value
        await CreateTestCheckin(context, metric.Id, org.Id, value: 70m,
            checkinDate: DateTime.UtcNow);

        var service = new MissionProgressService(context);
        var result = await service.GetProgressAsync([mission.Id]);

        result.Value![0].OverallProgress.Should().Be(70m);
    }

    [Fact]
    public async Task GetProgressAsync_CalculatesExpectedProgress()
    {
        var startDate = DateTime.UtcNow.AddDays(-10);
        var endDate = DateTime.UtcNow.AddDays(10);

        await using var context = CreateInMemoryContext();
        var (_, mission) = await CreateTestMission(context, startDate, endDate);
        var service = new MissionProgressService(context);

        var result = await service.GetProgressAsync([mission.Id]);

        // ~50% expected (10 of 20 days elapsed)
        result.Value![0].ExpectedProgress.Should().BeApproximately(50m, 1m);
    }

    [Fact]
    public async Task GetProgressAsync_MultipleMissions_ReturnsAll()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission1) = await CreateTestMission(context);
        var mission2 = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Mission 2",
            StartDate = DateTime.UtcNow.AddDays(-5),
            EndDate = DateTime.UtcNow.AddDays(25),
            Status = MissionStatus.Active,
            OrganizationId = org.Id
        };
        context.Missions.Add(mission2);
        await context.SaveChangesAsync();

        var metric1 = await CreateTestMetric(context, mission1.Id, org.Id,
            quantitativeType: QuantitativeMetricType.Achieve, maxValue: 100m);
        await CreateTestCheckin(context, metric1.Id, org.Id, value: 50m);

        var metric2 = await CreateTestMetric(context, mission2.Id, org.Id,
            quantitativeType: QuantitativeMetricType.Achieve, maxValue: 200m);
        await CreateTestCheckin(context, metric2.Id, org.Id, value: 100m);

        var service = new MissionProgressService(context);
        var result = await service.GetProgressAsync([mission1.Id, mission2.Id]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value!.First(p => p.MissionId == mission1.Id).OverallProgress.Should().Be(50m);
        result.Value!.First(p => p.MissionId == mission2.Id).OverallProgress.Should().Be(50m);
    }

    [Fact]
    public void CalculateExpectedProgress_BeforeStart_Returns0()
    {
        var result = MissionProgressService.CalculateExpectedProgress(
            DateTime.UtcNow.AddDays(5),
            DateTime.UtcNow.AddDays(15),
            DateTime.UtcNow);

        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateExpectedProgress_AfterEnd_Returns100()
    {
        var result = MissionProgressService.CalculateExpectedProgress(
            DateTime.UtcNow.AddDays(-20),
            DateTime.UtcNow.AddDays(-5),
            DateTime.UtcNow);

        result.Should().Be(100m);
    }

    [Fact]
    public async Task GetProgressAsync_MetricWithMixedCheckinTypes_OnlyCountsMetricsWithCheckins()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);

        // Metric 1: has check-in → 60%
        var metric1 = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.Achieve, maxValue: 100m);
        await CreateTestCheckin(context, metric1.Id, org.Id, value: 60m, confidenceLevel: 4);

        // Metric 2: no check-in → 0%
        await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.Achieve, maxValue: 200m);

        var service = new MissionProgressService(context);
        var result = await service.GetProgressAsync([mission.Id]);

        var progress = result.Value![0];
        // 60% from metric1 + 0% from metric2 (no checkin), averaged over 2 total metrics = 30%
        progress.OverallProgress.Should().Be(30m);
        progress.TotalMetrics.Should().Be(2);
        progress.MetricsWithCheckins.Should().Be(1);
        progress.AverageConfidence.Should().Be(4m);
    }

    [Fact]
    public async Task GetProgressAsync_AchieveMetric_NegativeValue_ClampsAt0()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.Achieve, maxValue: 100m);
        await CreateTestCheckin(context, metric.Id, org.Id, value: -10m);
        var service = new MissionProgressService(context);

        var result = await service.GetProgressAsync([mission.Id]);

        result.Value![0].OverallProgress.Should().Be(0m);
    }

    [Fact]
    public async Task GetProgressAsync_KeepAboveMetric_NegativeValue_Returns0()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.KeepAbove, minValue: 10m, maxValue: null);
        await CreateTestCheckin(context, metric.Id, org.Id, value: -5m);
        var service = new MissionProgressService(context);

        var result = await service.GetProgressAsync([mission.Id]);

        result.Value![0].OverallProgress.Should().Be(0m);
    }

    [Fact]
    public async Task GetProgressAsync_KeepBelowMetric_FarAboveThreshold_ReturnsLowProportional()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.KeepBelow, minValue: null, maxValue: 50m);
        await CreateTestCheckin(context, metric.Id, org.Id, value: 200m);
        var service = new MissionProgressService(context);

        var result = await service.GetProgressAsync([mission.Id]);

        // 50/200 * 100 = 25%
        result.Value![0].OverallProgress.Should().Be(25m);
    }

    [Fact]
    public async Task GetProgressAsync_KeepBetweenMetric_AboveRange_ReturnsProportional()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.KeepBetween, minValue: 80m, maxValue: 95m);
        await CreateTestCheckin(context, metric.Id, org.Id, value: 100m);
        var service = new MissionProgressService(context);

        var result = await service.GetProgressAsync([mission.Id]);

        // 95/100 * 100 = 95%
        result.Value![0].OverallProgress.Should().Be(95m);
    }

    [Fact]
    public async Task GetProgressAsync_KeepAbove_MultipleCheckinsOnSameDate_UsesLatestInserted()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.KeepAbove, minValue: 10m, maxValue: null);

        var sameDate = DateTime.UtcNow.Date;

        // First check-in: below threshold
        await CreateTestCheckin(context, metric.Id, org.Id, value: 8m,
            checkinDate: sameDate);
        // Second check-in (same date): above threshold — this should be used
        await CreateTestCheckin(context, metric.Id, org.Id, value: 11m,
            checkinDate: sameDate);

        var service = new MissionProgressService(context);
        var result = await service.GetProgressAsync([mission.Id]);

        // Should use value=11 (latest inserted), 11 >= 10 → 100%
        result.Value![0].OverallProgress.Should().Be(100m);
    }

    [Fact]
    public async Task GetProgressAsync_Achieve_MultipleCheckinsOnSameDate_UsesLatestInserted()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.Achieve, maxValue: 100m);

        var sameDate = DateTime.UtcNow.Date;

        // First check-in: 20%
        await CreateTestCheckin(context, metric.Id, org.Id, value: 20m,
            checkinDate: sameDate);
        // Second check-in (same date): 80% — this should be used
        await CreateTestCheckin(context, metric.Id, org.Id, value: 80m,
            checkinDate: sameDate);

        var service = new MissionProgressService(context);
        var result = await service.GetProgressAsync([mission.Id]);

        // Should use value=80 (latest inserted), 80/100 = 80%
        result.Value![0].OverallProgress.Should().Be(80m);
    }

    [Fact]
    public async Task GetProgressAsync_KeepBelow_MultipleCheckinsOnSameDate_UsesLatestInserted()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.KeepBelow, minValue: null, maxValue: 50m);

        var sameDate = DateTime.UtcNow.Date;

        // First check-in: above threshold → 0%
        await CreateTestCheckin(context, metric.Id, org.Id, value: 60m,
            checkinDate: sameDate);
        // Second check-in (same date): below threshold → 100%
        await CreateTestCheckin(context, metric.Id, org.Id, value: 40m,
            checkinDate: sameDate);

        var service = new MissionProgressService(context);
        var result = await service.GetProgressAsync([mission.Id]);

        // Should use value=40 (latest inserted), 40 <= 50 → 100%
        result.Value![0].OverallProgress.Should().Be(100m);
    }

    [Fact]
    public async Task GetProgressAsync_KeepBetween_MultipleCheckinsOnSameDate_UsesLatestInserted()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.KeepBetween, minValue: 80m, maxValue: 95m);

        var sameDate = DateTime.UtcNow.Date;

        // First check-in: out of range
        await CreateTestCheckin(context, metric.Id, org.Id, value: 70m,
            checkinDate: sameDate);
        // Second check-in (same date): in range
        await CreateTestCheckin(context, metric.Id, org.Id, value: 88m,
            checkinDate: sameDate);

        var service = new MissionProgressService(context);
        var result = await service.GetProgressAsync([mission.Id]);

        // Should use value=88 (latest inserted), 80 <= 88 <= 95 → 100%
        result.Value![0].OverallProgress.Should().Be(100m);
    }

    [Fact]
    public async Task GetProgressAsync_Reduce_MultipleCheckinsOnSameDate_UsesLatestInserted()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.Reduce, maxValue: 10m);

        // Baseline from a different day
        await CreateTestCheckin(context, metric.Id, org.Id, value: 50m,
            checkinDate: DateTime.UtcNow.AddDays(-5));

        var sameDate = DateTime.UtcNow.Date;

        // Two check-ins on same day
        await CreateTestCheckin(context, metric.Id, org.Id, value: 45m,
            checkinDate: sameDate);
        await CreateTestCheckin(context, metric.Id, org.Id, value: 20m,
            checkinDate: sameDate);

        var service = new MissionProgressService(context);
        var result = await service.GetProgressAsync([mission.Id]);

        // baseline=50, latest=20, target=10 → (50-20)/(50-10) = 30/40 = 75%
        result.Value![0].OverallProgress.Should().Be(75m);
    }

    [Fact]
    public async Task GetProgressAsync_Qualitative_MultipleCheckins_UsesLatestConfidence()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            type: MetricType.Qualitative);

        // Old check-in with low confidence
        await CreateTestCheckin(context, metric.Id, org.Id, text: "Started",
            confidenceLevel: 1, checkinDate: DateTime.UtcNow.AddDays(-3));
        // Latest check-in with high confidence
        await CreateTestCheckin(context, metric.Id, org.Id, text: "Good progress",
            confidenceLevel: 4, checkinDate: DateTime.UtcNow);

        var service = new MissionProgressService(context);
        var result = await service.GetProgressAsync([mission.Id]);

        // Should use confidence=4 (latest), 4/5 * 100 = 80%
        result.Value![0].OverallProgress.Should().Be(80m);
        result.Value![0].AverageConfidence.Should().Be(4m);
    }

    [Fact]
    public async Task GetProgressAsync_Qualitative_MultipleCheckinsOnSameDate_UsesLatestInserted()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            type: MetricType.Qualitative);

        var sameDate = DateTime.UtcNow.Date;

        // First check-in: low confidence
        await CreateTestCheckin(context, metric.Id, org.Id, text: "Bad",
            confidenceLevel: 1, checkinDate: sameDate);
        // Second check-in (same date): high confidence
        await CreateTestCheckin(context, metric.Id, org.Id, text: "Great",
            confidenceLevel: 5, checkinDate: sameDate);

        var service = new MissionProgressService(context);
        var result = await service.GetProgressAsync([mission.Id]);

        // Should use confidence=5 (latest inserted), 5/5 * 100 = 100%
        result.Value![0].OverallProgress.Should().Be(100m);
        result.Value![0].AverageConfidence.Should().Be(5m);
    }

    [Fact]
    public async Task GetProgressAsync_Achieve_ThreeCheckinsProgressiveEvolution()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.Achieve, maxValue: 100m);

        await CreateTestCheckin(context, metric.Id, org.Id, value: 10m,
            checkinDate: DateTime.UtcNow.AddDays(-10));
        await CreateTestCheckin(context, metric.Id, org.Id, value: 40m,
            checkinDate: DateTime.UtcNow.AddDays(-5));
        await CreateTestCheckin(context, metric.Id, org.Id, value: 90m,
            checkinDate: DateTime.UtcNow);

        var service = new MissionProgressService(context);
        var result = await service.GetProgressAsync([mission.Id]);

        // Should use latest value=90 → 90%
        result.Value![0].OverallProgress.Should().Be(90m);
    }

    [Fact]
    public async Task GetProgressAsync_MultipleMetrics_EachWithMultipleCheckins()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);

        // Metric 1: Achieve 200, two check-ins → latest = 120 → 60%
        var metric1 = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.Achieve, maxValue: 200m);
        await CreateTestCheckin(context, metric1.Id, org.Id, value: 50m,
            confidenceLevel: 2, checkinDate: DateTime.UtcNow.AddDays(-3));
        await CreateTestCheckin(context, metric1.Id, org.Id, value: 120m,
            confidenceLevel: 4, checkinDate: DateTime.UtcNow);

        // Metric 2: KeepAbove 70, two check-ins → latest = 80 → 100%
        var metric2 = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.KeepAbove, minValue: 70m, maxValue: null);
        await CreateTestCheckin(context, metric2.Id, org.Id, value: 60m,
            confidenceLevel: 1, checkinDate: DateTime.UtcNow.AddDays(-2));
        await CreateTestCheckin(context, metric2.Id, org.Id, value: 80m,
            confidenceLevel: 3, checkinDate: DateTime.UtcNow);

        var service = new MissionProgressService(context);
        var result = await service.GetProgressAsync([mission.Id]);

        var progress = result.Value![0];
        // (60 + 100) / 2 = 80%
        progress.OverallProgress.Should().Be(80m);
        progress.TotalMetrics.Should().Be(2);
        progress.MetricsWithCheckins.Should().Be(2);
        // Confidence uses latest check-in per metric: (4 + 3) / 2 = 3.5
        progress.AverageConfidence.Should().Be(3.5m);
    }

    [Fact]
    public async Task GetProgressAsync_Achieve_LatestCheckinIsLowerThanPrevious()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.Achieve, maxValue: 100m);

        // Higher value first
        await CreateTestCheckin(context, metric.Id, org.Id, value: 80m,
            checkinDate: DateTime.UtcNow.AddDays(-2));
        // Latest has lower value (regression)
        await CreateTestCheckin(context, metric.Id, org.Id, value: 30m,
            checkinDate: DateTime.UtcNow);

        var service = new MissionProgressService(context);
        var result = await service.GetProgressAsync([mission.Id]);

        // Should use latest value=30 (even though it's lower) → 30%
        result.Value![0].OverallProgress.Should().Be(30m);
    }

    [Fact]
    public async Task GetProgressAsync_Reduce_BaselineIsFirstCheckin_NotLowest()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.Reduce, maxValue: 10m);

        // First check-in (baseline) = 40
        await CreateTestCheckin(context, metric.Id, org.Id, value: 40m,
            checkinDate: DateTime.UtcNow.AddDays(-10));
        // Intermediate goes up to 60 (regression)
        await CreateTestCheckin(context, metric.Id, org.Id, value: 60m,
            checkinDate: DateTime.UtcNow.AddDays(-5));
        // Latest = 25
        await CreateTestCheckin(context, metric.Id, org.Id, value: 25m,
            checkinDate: DateTime.UtcNow);

        var service = new MissionProgressService(context);
        var result = await service.GetProgressAsync([mission.Id]);

        // baseline=40, latest=25, target=10 → (40-25)/(40-10) = 15/30 = 50%
        result.Value![0].OverallProgress.Should().Be(50m);
    }

    // ---- GetMetricProgressAsync Tests ----

    [Fact]
    public async Task GetMetricProgress_EmptyList_ReturnsEmpty()
    {
        await using var context = CreateInMemoryContext();
        var service = new MissionProgressService(context);

        var result = await service.GetMetricProgressAsync([]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMetricProgress_NoCheckins_ReturnsZeroProgressWithHasCheckinsFalse()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id, maxValue: 100m);

        var service = new MissionProgressService(context);
        var result = await service.GetMetricProgressAsync([metric.Id]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].MetricId.Should().Be(metric.Id);
        result.Value[0].Progress.Should().Be(0m);
        result.Value[0].Confidence.Should().Be(0);
        result.Value[0].HasCheckins.Should().BeFalse();
    }

    [Fact]
    public async Task GetMetricProgress_AchieveMetric_ReturnsCorrectProgress()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.Achieve, maxValue: 100m);
        await CreateTestCheckin(context, metric.Id, org.Id, value: 60m, confidenceLevel: 4);

        var service = new MissionProgressService(context);
        var result = await service.GetMetricProgressAsync([metric.Id]);

        result.Value![0].Progress.Should().Be(60m);
        result.Value[0].Confidence.Should().Be(4);
        result.Value[0].HasCheckins.Should().BeTrue();
    }

    [Fact]
    public async Task GetMetricProgress_QualitativeMetric_ReturnsConfidenceAsProgress()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            type: MetricType.Qualitative, maxValue: null);
        await CreateTestCheckin(context, metric.Id, org.Id, text: "Going well", confidenceLevel: 4);

        var service = new MissionProgressService(context);
        var result = await service.GetMetricProgressAsync([metric.Id]);

        result.Value![0].Progress.Should().Be(80m); // 4/5*100
        result.Value[0].Confidence.Should().Be(4);
        result.Value[0].HasCheckins.Should().BeTrue();
    }

    [Fact]
    public async Task GetMetricProgress_MultipleMetrics_ReturnsAllResults()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric1 = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.Achieve, maxValue: 100m);
        var metric2 = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.KeepAbove, minValue: 70m, maxValue: null);

        await CreateTestCheckin(context, metric1.Id, org.Id, value: 50m, confidenceLevel: 3);
        await CreateTestCheckin(context, metric2.Id, org.Id, value: 80m, confidenceLevel: 5);

        var service = new MissionProgressService(context);
        var result = await service.GetMetricProgressAsync([metric1.Id, metric2.Id]);

        result.Value.Should().NotBeNull();
        var progresses = result.Value!;
        progresses.Should().HaveCount(2);

        var progress1 = progresses.First(p => p.MetricId == metric1.Id);
        progress1.Progress.Should().Be(50m);
        progress1.Confidence.Should().Be(3);

        var progress2 = progresses.First(p => p.MetricId == metric2.Id);
        progress2.Progress.Should().Be(100m); // 80 >= 70
        progress2.Confidence.Should().Be(5);
    }

    [Fact]
    public async Task GetMetricProgress_ReduceMetric_UsesFirstCheckinAsBaseline()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.Reduce, maxValue: 10m);

        await CreateTestCheckin(context, metric.Id, org.Id, value: 50m, confidenceLevel: 2,
            checkinDate: DateTime.UtcNow.AddDays(-10));
        await CreateTestCheckin(context, metric.Id, org.Id, value: 30m, confidenceLevel: 3,
            checkinDate: DateTime.UtcNow);

        var service = new MissionProgressService(context);
        var result = await service.GetMetricProgressAsync([metric.Id]);

        // baseline=50, current=30, target=10 → (50-30)/(50-10) = 20/40 = 50%
        result.Value![0].Progress.Should().Be(50m);
        result.Value[0].Confidence.Should().Be(3);
    }

    [Fact]
    public async Task GetMetricProgress_WithCheckin_ReturnsCollaboratorName()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.Achieve, maxValue: 100m);
        await CreateTestCheckin(context, metric.Id, org.Id, value: 50m,
            collaboratorName: "João Silva");

        var service = new MissionProgressService(context);
        var result = await service.GetMetricProgressAsync([metric.Id]);

        result.IsSuccess.Should().BeTrue();
        result.Value![0].LastCheckinCollaboratorName.Should().Be("João Silva");
    }

    [Fact]
    public async Task GetMetricProgress_NoCheckins_ReturnsNullCollaboratorName()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id, maxValue: 100m);

        var service = new MissionProgressService(context);
        var result = await service.GetMetricProgressAsync([metric.Id]);

        result.IsSuccess.Should().BeTrue();
        result.Value![0].LastCheckinCollaboratorName.Should().BeNull();
    }

    [Fact]
    public async Task GetMetricProgress_MultipleCheckins_ReturnsLatestCollaboratorName()
    {
        await using var context = CreateInMemoryContext();
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id,
            quantitativeType: QuantitativeMetricType.Achieve, maxValue: 100m);

        await CreateTestCheckin(context, metric.Id, org.Id, value: 20m,
            checkinDate: DateTime.UtcNow.AddDays(-5), collaboratorName: "Maria Santos");
        await CreateTestCheckin(context, metric.Id, org.Id, value: 70m,
            checkinDate: DateTime.UtcNow, collaboratorName: "Pedro Oliveira");

        var service = new MissionProgressService(context);
        var result = await service.GetMetricProgressAsync([metric.Id]);

        result.IsSuccess.Should().BeTrue();
        result.Value![0].LastCheckinCollaboratorName.Should().Be("Pedro Oliveira");
    }
}
