using Bud.Server.Data;
using Bud.Server.Services;
using Bud.Server.Tests.Helpers;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bud.Server.Tests.Services;

public class MissionMetricServiceTests
{
    private readonly TestTenantProvider _tenantProvider = new() { IsAdmin = true };

    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, _tenantProvider);
    }

    private async Task<Mission> CreateTestMission(ApplicationDbContext context)
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

    #region ApplyMetricTarget Tests

    [Fact]
    public async Task ApplyMetricTarget_WithQualitativeType_SetsOnlyTargetText()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MissionMetricService(context);
        var mission = await CreateTestMission(context);

        var request = new CreateMissionMetricRequest
        {
            MissionId = mission.Id,
            Name = "Qualitative Metric",
            Type = MetricType.Qualitative,
            TargetText = "Complete all requirements"
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TargetText.Should().Be("Complete all requirements");
        result.Value!.QuantitativeType.Should().BeNull();
        result.Value!.MinValue.Should().BeNull();
        result.Value!.MaxValue.Should().BeNull();
        result.Value!.Unit.Should().BeNull();
    }

    [Fact]
    public async Task ApplyMetricTarget_WithQualitativeType_NullsQuantitativeFields()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MissionMetricService(context);
        var mission = await CreateTestMission(context);

        var request = new CreateMissionMetricRequest
        {
            MissionId = mission.Id,
            Name = "Qualitative Metric",
            Type = MetricType.Qualitative,
            TargetText = "Description",
            QuantitativeType = QuantitativeMetricType.KeepAbove, // Should be ignored
            MinValue = 100m, // Should be ignored
            Unit = MetricUnit.Integer // Should be ignored
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TargetText.Should().Be("Description");
        result.Value!.QuantitativeType.Should().BeNull(); // Nulled out
        result.Value!.MinValue.Should().BeNull(); // Nulled out
        result.Value!.MaxValue.Should().BeNull(); // Nulled out
        result.Value!.Unit.Should().BeNull(); // Nulled out
    }

    [Fact]
    public async Task ApplyMetricTarget_WithKeepAbove_SetsMinValueAndUnit()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MissionMetricService(context);
        var mission = await CreateTestMission(context);

        var request = new CreateMissionMetricRequest
        {
            MissionId = mission.Id,
            Name = "Story Points",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.KeepAbove,
            MinValue = 100m,
            Unit = MetricUnit.Points
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.QuantitativeType.Should().Be(QuantitativeMetricType.KeepAbove);
        result.Value!.MinValue.Should().Be(100m);
        result.Value!.MaxValue.Should().BeNull();
        result.Value!.Unit.Should().Be(MetricUnit.Points);
        result.Value!.TargetText.Should().BeNull();
    }

    [Fact]
    public async Task ApplyMetricTarget_WithKeepBelow_SetsMaxValueAndUnit()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MissionMetricService(context);
        var mission = await CreateTestMission(context);

        var request = new CreateMissionMetricRequest
        {
            MissionId = mission.Id,
            Name = "Error Rate",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.KeepBelow,
            MaxValue = 5m,
            Unit = MetricUnit.Percentage
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.QuantitativeType.Should().Be(QuantitativeMetricType.KeepBelow);
        result.Value!.MinValue.Should().BeNull();
        result.Value!.MaxValue.Should().Be(5m);
        result.Value!.Unit.Should().Be(MetricUnit.Percentage);
        result.Value!.TargetText.Should().BeNull();
    }

    [Fact]
    public async Task ApplyMetricTarget_WithKeepBetween_SetsBothValuesAndUnit()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MissionMetricService(context);
        var mission = await CreateTestMission(context);

        var request = new CreateMissionMetricRequest
        {
            MissionId = mission.Id,
            Name = "Response Time",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.KeepBetween,
            MinValue = 100m,
            MaxValue = 500m,
            Unit = MetricUnit.Integer
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.QuantitativeType.Should().Be(QuantitativeMetricType.KeepBetween);
        result.Value!.MinValue.Should().Be(100m);
        result.Value!.MaxValue.Should().Be(500m);
        result.Value!.Unit.Should().Be(MetricUnit.Integer);
        result.Value!.TargetText.Should().BeNull();
    }

    [Fact]
    public async Task ApplyMetricTarget_WithQuantitativeType_NullsQualitativeFields()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MissionMetricService(context);
        var mission = await CreateTestMission(context);

        var request = new CreateMissionMetricRequest
        {
            MissionId = mission.Id,
            Name = "Quantitative Metric",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.KeepAbove,
            MinValue = 85m,
            Unit = MetricUnit.Percentage,
            TargetText = "This should be ignored" // Should be ignored
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.QuantitativeType.Should().Be(QuantitativeMetricType.KeepAbove);
        result.Value!.MinValue.Should().Be(85m);
        result.Value!.Unit.Should().Be(MetricUnit.Percentage);
        result.Value!.TargetText.Should().BeNull(); // Nulled out
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateMetric_WithNonExistentMission_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MissionMetricService(context);

        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(), // Non-existent mission
            Name = "Test Metric",
            Type = MetricType.Qualitative,
            TargetText = "Description"
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Mission not found.");
    }

    [Fact]
    public async Task CreateMetric_WithValidQualitativeMetric_CreatesCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MissionMetricService(context);
        var mission = await CreateTestMission(context);

        var request = new CreateMissionMetricRequest
        {
            MissionId = mission.Id,
            Name = "Quality Assessment",
            Type = MetricType.Qualitative,
            TargetText = "Achieve excellent quality"
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().NotBeEmpty();
        result.Value!.MissionId.Should().Be(mission.Id);
        result.Value!.Name.Should().Be("Quality Assessment");
        result.Value!.Type.Should().Be(MetricType.Qualitative);
        result.Value!.TargetText.Should().Be("Achieve excellent quality");
    }

    [Fact]
    public async Task CreateMetric_WithValidQuantitativeMetric_CreatesCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MissionMetricService(context);
        var mission = await CreateTestMission(context);

        var request = new CreateMissionMetricRequest
        {
            MissionId = mission.Id,
            Name = "Story Points",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.KeepAbove,
            MinValue = 50m,
            Unit = MetricUnit.Points
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().NotBeEmpty();
        result.Value!.MissionId.Should().Be(mission.Id);
        result.Value!.Name.Should().Be("Story Points");
        result.Value!.Type.Should().Be(MetricType.Quantitative);
        result.Value!.QuantitativeType.Should().Be(QuantitativeMetricType.KeepAbove);
        result.Value!.MinValue.Should().Be(50m);
        result.Value!.Unit.Should().Be(MetricUnit.Points);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateMetric_ChangingTypeFromQualitativeToQuantitative_ClearsTargetText()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MissionMetricService(context);
        var mission = await CreateTestMission(context);

        // Create initial qualitative metric
        var createRequest = new CreateMissionMetricRequest
        {
            MissionId = mission.Id,
            Name = "Initial Metric",
            Type = MetricType.Qualitative,
            TargetText = "Original description"
        };

        var createResult = await service.CreateAsync(createRequest);
        var metricId = createResult.Value!.Id;

        // Act - Update to quantitative
        var updateRequest = new UpdateMissionMetricRequest
        {
            Name = "Updated Metric",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.KeepBelow,
            MaxValue = 75m,
            Unit = MetricUnit.Percentage
        };

        var updateResult = await service.UpdateAsync(metricId, updateRequest);

        // Assert
        updateResult.IsSuccess.Should().BeTrue();
        updateResult.Value!.Type.Should().Be(MetricType.Quantitative);
        updateResult.Value!.QuantitativeType.Should().Be(QuantitativeMetricType.KeepBelow);
        updateResult.Value!.MaxValue.Should().Be(75m);
        updateResult.Value!.Unit.Should().Be(MetricUnit.Percentage);
        updateResult.Value!.TargetText.Should().BeNull(); // Cleared when changed to quantitative
    }

    #endregion
}
