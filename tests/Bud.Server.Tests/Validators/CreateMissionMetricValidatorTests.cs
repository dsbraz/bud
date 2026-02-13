using Bud.Server.Validators;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
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
            e.ErrorMessage.Contains("métricas qualitativas"));
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
            e.ErrorMessage.Contains("métricas qualitativas"));
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
    public async Task Validate_QuantitativeWithoutQuantitativeType_Fails()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Story Points",
            Type = MetricType.Quantitative,
            QuantitativeType = null, // Missing QuantitativeType
            MinValue = 50m,
            Unit = MetricUnit.Points
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "QuantitativeType" &&
            e.ErrorMessage.Contains("métricas quantitativas"));
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
            QuantitativeType = QuantitativeMetricType.KeepAbove,
            MinValue = 50m,
            Unit = null // Missing Unit
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "Unit" &&
            e.ErrorMessage.Contains("métricas quantitativas"));
    }

    #region KeepAbove Tests

    [Fact]
    public async Task Validate_KeepAboveWithValidMinValue_Passes()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Story Points",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.KeepAbove,
            MinValue = 50m,
            Unit = MetricUnit.Points
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_KeepAboveWithoutMinValue_Fails()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Story Points",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.KeepAbove,
            MinValue = null, // Missing MinValue
            Unit = MetricUnit.Points
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "MinValue" &&
            e.ErrorMessage.Contains("KeepAbove"));
    }

    [Fact]
    public async Task Validate_KeepAboveWithNegativeMinValue_Fails()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Story Points",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.KeepAbove,
            MinValue = -10m, // Negative value
            Unit = MetricUnit.Points
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "MinValue" &&
            e.ErrorMessage.Contains("maior ou igual a 0"));
    }

    [Fact]
    public async Task Validate_KeepAboveWithZeroMinValue_Passes()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Story Points",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.KeepAbove,
            MinValue = 0m, // Zero is valid
            Unit = MetricUnit.Points
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    #endregion

    #region KeepBelow Tests

    [Fact]
    public async Task Validate_KeepBelowWithValidMaxValue_Passes()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Error Rate",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.KeepBelow,
            MaxValue = 5m,
            Unit = MetricUnit.Percentage
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_KeepBelowWithoutMaxValue_Fails()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Error Rate",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.KeepBelow,
            MaxValue = null, // Missing MaxValue
            Unit = MetricUnit.Percentage
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "MaxValue" &&
            e.ErrorMessage.Contains("KeepBelow"));
    }

    [Fact]
    public async Task Validate_KeepBelowWithNegativeMaxValue_Fails()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Error Rate",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.KeepBelow,
            MaxValue = -5m, // Negative value
            Unit = MetricUnit.Percentage
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "MaxValue" &&
            e.ErrorMessage.Contains("maior ou igual a 0"));
    }

    #endregion

    #region KeepBetween Tests

    [Fact]
    public async Task Validate_KeepBetweenWithValidValues_Passes()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Response Time",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.KeepBetween,
            MinValue = 100m,
            MaxValue = 500m,
            Unit = MetricUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_KeepBetweenWithoutMinValue_Fails()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Response Time",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.KeepBetween,
            MinValue = null, // Missing MinValue
            MaxValue = 500m,
            Unit = MetricUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "MinValue" &&
            e.ErrorMessage.Contains("KeepBetween"));
    }

    [Fact]
    public async Task Validate_KeepBetweenWithoutMaxValue_Fails()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Response Time",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.KeepBetween,
            MinValue = 100m,
            MaxValue = null, // Missing MaxValue
            Unit = MetricUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "MaxValue" &&
            e.ErrorMessage.Contains("KeepBetween"));
    }

    [Fact]
    public async Task Validate_KeepBetweenWithMinValueGreaterThanMaxValue_Fails()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Response Time",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.KeepBetween,
            MinValue = 500m, // Greater than MaxValue
            MaxValue = 100m,
            Unit = MetricUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "MaxValue" &&
            e.ErrorMessage.Contains("valor mínimo"));
    }

    [Fact]
    public async Task Validate_KeepBetweenWithEqualMinAndMaxValues_Fails()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Response Time",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.KeepBetween,
            MinValue = 100m, // Equal to MaxValue
            MaxValue = 100m,
            Unit = MetricUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "MaxValue" &&
            e.ErrorMessage.Contains("valor mínimo"));
    }

    [Fact]
    public async Task Validate_KeepBetweenWithNegativeMinValue_Fails()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Response Time",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.KeepBetween,
            MinValue = -10m, // Negative value
            MaxValue = 100m,
            Unit = MetricUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "MinValue" &&
            e.ErrorMessage.Contains("maior ou igual a 0"));
    }

    #endregion

    #region Achieve Tests

    [Fact]
    public async Task Validate_AchieveWithValidMaxValue_Passes()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Sales Target",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.Achieve,
            MaxValue = 100m,
            Unit = MetricUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_AchieveWithoutMaxValue_Fails()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Sales Target",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.Achieve,
            MaxValue = null, // Missing MaxValue
            Unit = MetricUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "MaxValue" &&
            e.ErrorMessage.Contains("Achieve"));
    }

    [Fact]
    public async Task Validate_AchieveWithNegativeMaxValue_Fails()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Sales Target",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.Achieve,
            MaxValue = -50m, // Negative value
            Unit = MetricUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "MaxValue" &&
            e.ErrorMessage.Contains("maior ou igual a 0"));
    }

    #endregion

    #region Reduce Tests

    [Fact]
    public async Task Validate_ReduceWithValidMaxValue_Passes()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Cost Reduction",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.Reduce,
            MaxValue = 50m,
            Unit = MetricUnit.Percentage
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_ReduceWithoutMaxValue_Fails()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Cost Reduction",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.Reduce,
            MaxValue = null, // Missing MaxValue
            Unit = MetricUnit.Percentage
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "MaxValue" &&
            e.ErrorMessage.Contains("Reduce"));
    }

    [Fact]
    public async Task Validate_ReduceWithNegativeMaxValue_Fails()
    {
        // Arrange
        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Cost Reduction",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.Reduce,
            MaxValue = -10m, // Negative value
            Unit = MetricUnit.Percentage
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "MaxValue" &&
            e.ErrorMessage.Contains("maior ou igual a 0"));
    }

    #endregion

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
