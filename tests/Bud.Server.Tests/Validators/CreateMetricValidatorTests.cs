using Bud.Server.Validators;
using Bud.Shared.Contracts;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Validators;

public class CreateMetricValidatorTests
{
    private readonly CreateIndicatorValidator _validator = new();

    #region Qualitative Metric Validation Tests

    [Fact]
    public async Task Validate_QualitativeWithTargetText_Passes()
    {
        // Arrange
        var request = new CreateIndicatorRequest
        {
            GoalId = Guid.NewGuid(),
            Name = "Quality Metric",
            Type = Bud.Shared.Contracts.IndicatorType.Qualitative,
            TargetText = "Achieve high quality standards"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_QualitativeWithoutTargetText_Passes()
    {
        // Arrange
        var request = new CreateIndicatorRequest
        {
            GoalId = Guid.NewGuid(),
            Name = "Quality Metric",
            Type = Bud.Shared.Contracts.IndicatorType.Qualitative,
            TargetText = null
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_QualitativeWithEmptyTargetText_Passes()
    {
        // Arrange
        var request = new CreateIndicatorRequest
        {
            GoalId = Guid.NewGuid(),
            Name = "Quality Metric",
            Type = Bud.Shared.Contracts.IndicatorType.Qualitative,
            TargetText = ""
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_QualitativeWithTargetTextExceeding1000Chars_Fails()
    {
        // Arrange
        var request = new CreateIndicatorRequest
        {
            GoalId = Guid.NewGuid(),
            Name = "Quality Metric",
            Type = Bud.Shared.Contracts.IndicatorType.Qualitative,
            TargetText = new string('A', 1001) // 1001 characters
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("TargetText") &&
            e.ErrorMessage.Contains("1000"));
    }

    #endregion

    #region Quantitative Metric Validation Tests

    [Fact]
    public async Task Validate_QuantitativeWithoutQuantitativeType_Fails()
    {
        // Arrange
        var request = new CreateIndicatorRequest
        {
            GoalId = Guid.NewGuid(),
            Name = "Story Points",
            Type = Bud.Shared.Contracts.IndicatorType.Quantitative,
            QuantitativeType = null, // Missing QuantitativeType
            MinValue = 50m,
            Unit = Bud.Shared.Contracts.IndicatorUnit.Points
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("QuantitativeType") &&
            e.ErrorMessage.Contains("indicadores quantitativos"));
    }

    [Fact]
    public async Task Validate_QuantitativeWithoutUnit_Fails()
    {
        // Arrange
        var request = new CreateIndicatorRequest
        {
            GoalId = Guid.NewGuid(),
            Name = "Story Points",
            Type = Bud.Shared.Contracts.IndicatorType.Quantitative,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeIndicatorType.KeepAbove,
            MinValue = 50m,
            Unit = null // Missing Unit
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("Unit") &&
            e.ErrorMessage.Contains("indicadores quantitativos"));
    }

    #region KeepAbove Tests

    [Fact]
    public async Task Validate_KeepAboveWithValidMinValue_Passes()
    {
        // Arrange
        var request = new CreateIndicatorRequest
        {
            GoalId = Guid.NewGuid(),
            Name = "Story Points",
            Type = Bud.Shared.Contracts.IndicatorType.Quantitative,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeIndicatorType.KeepAbove,
            MinValue = 50m,
            Unit = Bud.Shared.Contracts.IndicatorUnit.Points
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
        var request = new CreateIndicatorRequest
        {
            GoalId = Guid.NewGuid(),
            Name = "Story Points",
            Type = Bud.Shared.Contracts.IndicatorType.Quantitative,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeIndicatorType.KeepAbove,
            MinValue = null, // Missing MinValue
            Unit = Bud.Shared.Contracts.IndicatorUnit.Points
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("MinValue") &&
            e.ErrorMessage.Contains("KeepAbove"));
    }

    [Fact]
    public async Task Validate_KeepAboveWithNegativeMinValue_Fails()
    {
        // Arrange
        var request = new CreateIndicatorRequest
        {
            GoalId = Guid.NewGuid(),
            Name = "Story Points",
            Type = Bud.Shared.Contracts.IndicatorType.Quantitative,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeIndicatorType.KeepAbove,
            MinValue = -10m, // Negative value
            Unit = Bud.Shared.Contracts.IndicatorUnit.Points
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("MinValue") &&
            e.ErrorMessage.Contains("maior ou igual a 0"));
    }

    [Fact]
    public async Task Validate_KeepAboveWithZeroMinValue_Passes()
    {
        // Arrange
        var request = new CreateIndicatorRequest
        {
            GoalId = Guid.NewGuid(),
            Name = "Story Points",
            Type = Bud.Shared.Contracts.IndicatorType.Quantitative,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeIndicatorType.KeepAbove,
            MinValue = 0m, // Zero is valid
            Unit = Bud.Shared.Contracts.IndicatorUnit.Points
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
        var request = new CreateIndicatorRequest
        {
            GoalId = Guid.NewGuid(),
            Name = "Error Rate",
            Type = Bud.Shared.Contracts.IndicatorType.Quantitative,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeIndicatorType.KeepBelow,
            MaxValue = 5m,
            Unit = Bud.Shared.Contracts.IndicatorUnit.Percentage
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
        var request = new CreateIndicatorRequest
        {
            GoalId = Guid.NewGuid(),
            Name = "Error Rate",
            Type = Bud.Shared.Contracts.IndicatorType.Quantitative,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeIndicatorType.KeepBelow,
            MaxValue = null, // Missing MaxValue
            Unit = Bud.Shared.Contracts.IndicatorUnit.Percentage
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("MaxValue") &&
            e.ErrorMessage.Contains("KeepBelow"));
    }

    [Fact]
    public async Task Validate_KeepBelowWithNegativeMaxValue_Fails()
    {
        // Arrange
        var request = new CreateIndicatorRequest
        {
            GoalId = Guid.NewGuid(),
            Name = "Error Rate",
            Type = Bud.Shared.Contracts.IndicatorType.Quantitative,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeIndicatorType.KeepBelow,
            MaxValue = -5m, // Negative value
            Unit = Bud.Shared.Contracts.IndicatorUnit.Percentage
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("MaxValue") &&
            e.ErrorMessage.Contains("maior ou igual a 0"));
    }

    #endregion

    #region KeepBetween Tests

    [Fact]
    public async Task Validate_KeepBetweenWithValidValues_Passes()
    {
        // Arrange
        var request = new CreateIndicatorRequest
        {
            GoalId = Guid.NewGuid(),
            Name = "Response Time",
            Type = Bud.Shared.Contracts.IndicatorType.Quantitative,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeIndicatorType.KeepBetween,
            MinValue = 100m,
            MaxValue = 500m,
            Unit = Bud.Shared.Contracts.IndicatorUnit.Integer
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
        var request = new CreateIndicatorRequest
        {
            GoalId = Guid.NewGuid(),
            Name = "Response Time",
            Type = Bud.Shared.Contracts.IndicatorType.Quantitative,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeIndicatorType.KeepBetween,
            MinValue = null, // Missing MinValue
            MaxValue = 500m,
            Unit = Bud.Shared.Contracts.IndicatorUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("MinValue") &&
            e.ErrorMessage.Contains("KeepBetween"));
    }

    [Fact]
    public async Task Validate_KeepBetweenWithoutMaxValue_Fails()
    {
        // Arrange
        var request = new CreateIndicatorRequest
        {
            GoalId = Guid.NewGuid(),
            Name = "Response Time",
            Type = Bud.Shared.Contracts.IndicatorType.Quantitative,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeIndicatorType.KeepBetween,
            MinValue = 100m,
            MaxValue = null, // Missing MaxValue
            Unit = Bud.Shared.Contracts.IndicatorUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("MaxValue") &&
            e.ErrorMessage.Contains("KeepBetween"));
    }

    [Fact]
    public async Task Validate_KeepBetweenWithMinValueGreaterThanMaxValue_Fails()
    {
        // Arrange
        var request = new CreateIndicatorRequest
        {
            GoalId = Guid.NewGuid(),
            Name = "Response Time",
            Type = Bud.Shared.Contracts.IndicatorType.Quantitative,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeIndicatorType.KeepBetween,
            MinValue = 500m, // Greater than MaxValue
            MaxValue = 100m,
            Unit = Bud.Shared.Contracts.IndicatorUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("MaxValue") &&
            e.ErrorMessage.Contains("valor mínimo"));
    }

    [Fact]
    public async Task Validate_KeepBetweenWithEqualMinAndMaxValues_Fails()
    {
        // Arrange
        var request = new CreateIndicatorRequest
        {
            GoalId = Guid.NewGuid(),
            Name = "Response Time",
            Type = Bud.Shared.Contracts.IndicatorType.Quantitative,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeIndicatorType.KeepBetween,
            MinValue = 100m, // Equal to MaxValue
            MaxValue = 100m,
            Unit = Bud.Shared.Contracts.IndicatorUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("MaxValue") &&
            e.ErrorMessage.Contains("valor mínimo"));
    }

    [Fact]
    public async Task Validate_KeepBetweenWithNegativeMinValue_Fails()
    {
        // Arrange
        var request = new CreateIndicatorRequest
        {
            GoalId = Guid.NewGuid(),
            Name = "Response Time",
            Type = Bud.Shared.Contracts.IndicatorType.Quantitative,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeIndicatorType.KeepBetween,
            MinValue = -10m, // Negative value
            MaxValue = 100m,
            Unit = Bud.Shared.Contracts.IndicatorUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("MinValue") &&
            e.ErrorMessage.Contains("maior ou igual a 0"));
    }

    #endregion

    #region Achieve Tests

    [Fact]
    public async Task Validate_AchieveWithValidMaxValue_Passes()
    {
        // Arrange
        var request = new CreateIndicatorRequest
        {
            GoalId = Guid.NewGuid(),
            Name = "Sales Target",
            Type = Bud.Shared.Contracts.IndicatorType.Quantitative,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeIndicatorType.Achieve,
            MaxValue = 100m,
            Unit = Bud.Shared.Contracts.IndicatorUnit.Integer
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
        var request = new CreateIndicatorRequest
        {
            GoalId = Guid.NewGuid(),
            Name = "Sales Target",
            Type = Bud.Shared.Contracts.IndicatorType.Quantitative,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeIndicatorType.Achieve,
            MaxValue = null, // Missing MaxValue
            Unit = Bud.Shared.Contracts.IndicatorUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("MaxValue") &&
            e.ErrorMessage.Contains("Achieve"));
    }

    [Fact]
    public async Task Validate_AchieveWithNegativeMaxValue_Fails()
    {
        // Arrange
        var request = new CreateIndicatorRequest
        {
            GoalId = Guid.NewGuid(),
            Name = "Sales Target",
            Type = Bud.Shared.Contracts.IndicatorType.Quantitative,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeIndicatorType.Achieve,
            MaxValue = -50m, // Negative value
            Unit = Bud.Shared.Contracts.IndicatorUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("MaxValue") &&
            e.ErrorMessage.Contains("maior ou igual a 0"));
    }

    #endregion

    #region Reduce Tests

    [Fact]
    public async Task Validate_ReduceWithValidMaxValue_Passes()
    {
        // Arrange
        var request = new CreateIndicatorRequest
        {
            GoalId = Guid.NewGuid(),
            Name = "Cost Reduction",
            Type = Bud.Shared.Contracts.IndicatorType.Quantitative,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeIndicatorType.Reduce,
            MaxValue = 50m,
            Unit = Bud.Shared.Contracts.IndicatorUnit.Percentage
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
        var request = new CreateIndicatorRequest
        {
            GoalId = Guid.NewGuid(),
            Name = "Cost Reduction",
            Type = Bud.Shared.Contracts.IndicatorType.Quantitative,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeIndicatorType.Reduce,
            MaxValue = null, // Missing MaxValue
            Unit = Bud.Shared.Contracts.IndicatorUnit.Percentage
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("MaxValue") &&
            e.ErrorMessage.Contains("Reduce"));
    }

    [Fact]
    public async Task Validate_ReduceWithNegativeMaxValue_Fails()
    {
        // Arrange
        var request = new CreateIndicatorRequest
        {
            GoalId = Guid.NewGuid(),
            Name = "Cost Reduction",
            Type = Bud.Shared.Contracts.IndicatorType.Quantitative,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeIndicatorType.Reduce,
            MaxValue = -10m, // Negative value
            Unit = Bud.Shared.Contracts.IndicatorUnit.Percentage
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("MaxValue") &&
            e.ErrorMessage.Contains("maior ou igual a 0"));
    }

    #endregion

    #endregion

    #region General Validation Tests

    [Fact]
    public async Task Validate_WithEmptyMissionId_Fails()
    {
        // Arrange
        var request = new CreateIndicatorRequest
        {
            GoalId = Guid.Empty, // Empty GUID
            Name = "Test Metric",
            Type = Bud.Shared.Contracts.IndicatorType.Qualitative,
            TargetText = "Description"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName.Contains("GoalId"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Validate_WithEmptyName_Fails(string? name)
    {
        // Arrange
        var request = new CreateIndicatorRequest
        {
            GoalId = Guid.NewGuid(),
            Name = name!,
            Type = Bud.Shared.Contracts.IndicatorType.Qualitative,
            TargetText = "Description"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Name"));
    }

    [Fact]
    public async Task Validate_WithNameExceeding200Chars_Fails()
    {
        // Arrange
        var request = new CreateIndicatorRequest
        {
            GoalId = Guid.NewGuid(),
            Name = new string('A', 201), // 201 characters
            Type = Bud.Shared.Contracts.IndicatorType.Qualitative,
            TargetText = "Description"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("Name") &&
            e.ErrorMessage.Contains("200"));
    }

    #endregion
}
