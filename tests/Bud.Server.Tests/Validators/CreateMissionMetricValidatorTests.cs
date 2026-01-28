using Bud.Server.Validators;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Validators;

public class CreateMissionMetricValidatorTests
{
    private readonly CreateMissionMetricValidator _validator = new();

    #region Qualitative Metric Validation Tests

    [Fact]
    public async Task Validate_QualitativeWithTargetText_Passes()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Quality Metric",
            Type = MetricType.Qualitative,
            TargetText = "Achieve high quality standards"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_QualitativeWithoutTargetText_Fails()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Quality Metric",
            Type = MetricType.Qualitative,
            TargetText = null // Missing TargetText
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "TargetText" &&
            e.ErrorMessage.Contains("qualitative"));
    }

    [Fact]
    public async Task Validate_QualitativeWithEmptyTargetText_Fails()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Quality Metric",
            Type = MetricType.Qualitative,
            TargetText = "" // Empty TargetText
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "TargetText" &&
            e.ErrorMessage.Contains("qualitative"));
    }

    [Fact]
    public async Task Validate_QualitativeWithTargetTextExceeding1000Chars_Fails()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Quality Metric",
            Type = MetricType.Qualitative,
            TargetText = new string('A', 1001) // 1001 characters
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "TargetText" &&
            e.ErrorMessage.Contains("1000"));
    }

    #endregion

    #region Quantitative Metric Validation Tests

    [Fact]
    public async Task Validate_QuantitativeWithValidData_Passes()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Story Points",
            Type = MetricType.Quantitative,
            TargetValue = 50m,
            Unit = MetricUnit.Points
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_QuantitativeWithoutTargetValue_Fails()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Story Points",
            Type = MetricType.Quantitative,
            TargetValue = null, // Missing TargetValue
            Unit = MetricUnit.Points
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "TargetValue" &&
            e.ErrorMessage.Contains("quantitative"));
    }

    [Fact]
    public async Task Validate_QuantitativeWithoutUnit_Fails()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Story Points",
            Type = MetricType.Quantitative,
            TargetValue = 50m,
            Unit = null // Missing Unit
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "Unit" &&
            e.ErrorMessage.Contains("quantitative"));
    }

    [Fact]
    public async Task Validate_QuantitativeWithNegativeTargetValue_Fails()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Story Points",
            Type = MetricType.Quantitative,
            TargetValue = -10m, // Negative value
            Unit = MetricUnit.Points
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "TargetValue" &&
            e.ErrorMessage.Contains("greater than or equal to 0"));
    }

    [Fact]
    public async Task Validate_QuantitativeWithZeroTargetValue_Passes()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Story Points",
            Type = MetricType.Quantitative,
            TargetValue = 0m, // Zero is valid
            Unit = MetricUnit.Points
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    #endregion

    #region General Validation Tests

    [Fact]
    public async Task Validate_WithEmptyMissionId_Fails()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.Empty, // Empty GUID
            Name = "Test Metric",
            Type = MetricType.Qualitative,
            TargetText = "Description"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "MissionId");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Validate_WithEmptyName_Fails(string? name)
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = name!,
            Type = MetricType.Qualitative,
            TargetText = "Description"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WithNameExceeding200Chars_Fails()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = new string('A', 201), // 201 characters
            Type = MetricType.Qualitative,
            TargetText = "Description"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "Name" &&
            e.ErrorMessage.Contains("200"));
    }

    #endregion
}
