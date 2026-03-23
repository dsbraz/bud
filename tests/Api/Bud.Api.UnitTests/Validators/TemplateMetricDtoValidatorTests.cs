using Bud.Shared.Contracts;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace Bud.Api.UnitTests.Validators;

public sealed class TemplateMetricDtoValidatorTests
{
    private readonly TemplateIndicatorDtoValidator _validator = new();

    #region General Validation Tests

    [Fact]
    public async Task Validate_ValidQuantitativeMetric_Passes()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Story Points",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.KeepAbove,
            MinValue = 50m,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Points
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_ValidQualitativeMetric_Passes()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Qualidade do Código",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Qualitative,
            OrderIndex = 0,
            TargetText = "Manter alta qualidade no código"
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Validate_EmptyName_Fails(string? name)
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = name!,
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Qualitative,
            OrderIndex = 0,
            TargetText = "Texto alvo"
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName.Contains("Name") &&
            e.ErrorMessage.Contains("Nome é obrigatório"));
    }

    [Fact]
    public async Task Validate_NameExceeding200Chars_Fails()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = new string('A', 201),
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Qualitative,
            OrderIndex = 0,
            TargetText = "Texto alvo"
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("Name") &&
            e.ErrorMessage.Contains("200"));
    }

    [Fact]
    public async Task Validate_NameExactly200Chars_Passes()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = new string('A', 200),
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Qualitative,
            OrderIndex = 0,
            TargetText = "Texto alvo"
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_InvalidType_Fails()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Métrica",
            Type = (Bud.Shared.Kernel.Enums.IndicatorType)99,
            OrderIndex = 0
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName.Contains("Type") &&
            e.ErrorMessage.Contains("Tipo inválido"));
    }

    [Fact]
    public async Task Validate_NegativeOrderIndex_Fails()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Métrica",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Qualitative,
            OrderIndex = -1,
            TargetText = "Texto alvo"
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("OrderIndex") &&
            e.ErrorMessage.Contains("maior ou igual a 0"));
    }

    [Fact]
    public async Task Validate_ZeroOrderIndex_Passes()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Métrica",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Qualitative,
            OrderIndex = 0,
            TargetText = "Texto alvo"
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Qualitative Metric Tests

    [Fact]
    public async Task Validate_QualitativeWithoutTargetText_Passes()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Métrica Qualitativa",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Qualitative,
            OrderIndex = 0,
            TargetText = null
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_QualitativeWithEmptyTargetText_Passes()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Métrica Qualitativa",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Qualitative,
            OrderIndex = 0,
            TargetText = ""
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_QualitativeWithTargetTextExceeding1000Chars_Fails()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Métrica Qualitativa",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Qualitative,
            OrderIndex = 0,
            TargetText = new string('A', 1001)
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("TargetText") &&
            e.ErrorMessage.Contains("1000"));
    }

    [Fact]
    public async Task Validate_QualitativeWithTargetTextExactly1000Chars_Passes()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Métrica Qualitativa",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Qualitative,
            OrderIndex = 0,
            TargetText = new string('A', 1000)
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Quantitative Metric - General Tests

    [Fact]
    public async Task Validate_QuantitativeWithoutQuantitativeType_Fails()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Métrica Quantitativa",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = null,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Points,
            MinValue = 50m
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName.Contains("QuantitativeType") &&
            e.ErrorMessage.Contains("indicadores quantitativos"));
    }

    [Fact]
    public async Task Validate_QuantitativeWithInvalidQuantitativeType_Fails()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Métrica Quantitativa",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = (Bud.Shared.Kernel.Enums.QuantitativeIndicatorType)99,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Points,
            MinValue = 50m
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName.Contains("QuantitativeType") &&
            e.ErrorMessage.Contains("Tipo quantitativo inválido"));
    }

    [Fact]
    public async Task Validate_QuantitativeWithoutUnit_Fails()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Métrica Quantitativa",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.KeepAbove,
            Unit = null,
            MinValue = 50m
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName.Contains("Unit") &&
            e.ErrorMessage.Contains("indicadores quantitativos"));
    }

    [Fact]
    public async Task Validate_QuantitativeWithInvalidUnit_Fails()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Métrica Quantitativa",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.KeepAbove,
            Unit = (Bud.Shared.Kernel.Enums.IndicatorUnit)99,
            MinValue = 50m
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName.Contains("Unit") &&
            e.ErrorMessage.Contains("Unidade inválida"));
    }

    #endregion

    #region KeepAbove Tests

    [Fact]
    public async Task Validate_KeepAboveWithValidMinValue_Passes()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Story Points",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.KeepAbove,
            MinValue = 50m,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Points
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_KeepAboveWithZeroMinValue_Passes()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Story Points",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.KeepAbove,
            MinValue = 0m,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Points
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_KeepAboveWithoutMinValue_Fails()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Story Points",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.KeepAbove,
            MinValue = null,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Points
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

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
        var dto = new TemplateIndicatorRequest
        {
            Name = "Story Points",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.KeepAbove,
            MinValue = -10m,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Points
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("MinValue") &&
            e.ErrorMessage.Contains("maior ou igual a 0"));
    }

    #endregion

    #region KeepBelow Tests

    [Fact]
    public async Task Validate_KeepBelowWithValidMaxValue_Passes()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Taxa de Erro",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.KeepBelow,
            MaxValue = 5m,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Percentage
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_KeepBelowWithZeroMaxValue_Passes()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Taxa de Erro",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.KeepBelow,
            MaxValue = 0m,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Percentage
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_KeepBelowWithoutMaxValue_Fails()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Taxa de Erro",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.KeepBelow,
            MaxValue = null,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Percentage
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

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
        var dto = new TemplateIndicatorRequest
        {
            Name = "Taxa de Erro",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.KeepBelow,
            MaxValue = -5m,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Percentage
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

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
        var dto = new TemplateIndicatorRequest
        {
            Name = "Tempo de Resposta",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.KeepBetween,
            MinValue = 100m,
            MaxValue = 500m,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_KeepBetweenWithoutMinValue_Fails()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Tempo de Resposta",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.KeepBetween,
            MinValue = null,
            MaxValue = 500m,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName.Contains("MinValue") &&
            e.ErrorMessage.Contains("KeepBetween"));
    }

    [Fact]
    public async Task Validate_KeepBetweenWithoutMaxValue_Fails()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Tempo de Resposta",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.KeepBetween,
            MinValue = 100m,
            MaxValue = null,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName.Contains("MaxValue") &&
            e.ErrorMessage.Contains("KeepBetween"));
    }

    [Fact]
    public async Task Validate_KeepBetweenWithMinValueGreaterThanMaxValue_Fails()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Tempo de Resposta",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.KeepBetween,
            MinValue = 500m,
            MaxValue = 100m,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName.Contains("MaxValue") &&
            e.ErrorMessage.Contains("valor mínimo"));
    }

    [Fact]
    public async Task Validate_KeepBetweenWithEqualMinAndMaxValues_Fails()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Tempo de Resposta",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.KeepBetween,
            MinValue = 100m,
            MaxValue = 100m,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName.Contains("MaxValue") &&
            e.ErrorMessage.Contains("valor mínimo"));
    }

    [Fact]
    public async Task Validate_KeepBetweenWithNegativeMinValue_Fails()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Tempo de Resposta",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.KeepBetween,
            MinValue = -10m,
            MaxValue = 100m,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName.Contains("MinValue") &&
            e.ErrorMessage.Contains("maior ou igual a 0"));
    }

    [Fact]
    public async Task Validate_KeepBetweenWithNegativeMaxValue_Fails()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Tempo de Resposta",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.KeepBetween,
            MinValue = 0m,
            MaxValue = -5m,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName.Contains("MaxValue") &&
            e.ErrorMessage.Contains("maior ou igual a 0"));
    }

    #endregion

    #region Achieve Tests

    [Fact]
    public async Task Validate_AchieveWithValidMaxValue_Passes()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Meta de Vendas",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.Achieve,
            MaxValue = 100m,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_AchieveWithZeroMaxValue_Passes()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Meta de Vendas",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.Achieve,
            MaxValue = 0m,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_AchieveWithoutMaxValue_Fails()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Meta de Vendas",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.Achieve,
            MaxValue = null,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

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
        var dto = new TemplateIndicatorRequest
        {
            Name = "Meta de Vendas",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.Achieve,
            MaxValue = -50m,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

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
        var dto = new TemplateIndicatorRequest
        {
            Name = "Redução de Custos",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.Reduce,
            MaxValue = 50m,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Percentage
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_ReduceWithZeroMaxValue_Passes()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Redução de Custos",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.Reduce,
            MaxValue = 0m,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Percentage
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ReduceWithoutMaxValue_Fails()
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Redução de Custos",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.Reduce,
            MaxValue = null,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Percentage
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

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
        var dto = new TemplateIndicatorRequest
        {
            Name = "Redução de Custos",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.Reduce,
            MaxValue = -10m,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Percentage
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("MaxValue") &&
            e.ErrorMessage.Contains("maior ou igual a 0"));
    }

    #endregion

    #region Unit Enum Tests

    [Theory]
    [InlineData(Bud.Shared.Kernel.Enums.IndicatorUnit.Integer)]
    [InlineData(Bud.Shared.Kernel.Enums.IndicatorUnit.Decimal)]
    [InlineData(Bud.Shared.Kernel.Enums.IndicatorUnit.Percentage)]
    [InlineData(Bud.Shared.Kernel.Enums.IndicatorUnit.Hours)]
    [InlineData(Bud.Shared.Kernel.Enums.IndicatorUnit.Points)]
    public async Task Validate_QuantitativeWithAllValidUnits_Passes(Bud.Shared.Kernel.Enums.IndicatorUnit unit)
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Métrica",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.Achieve,
            MaxValue = 100m,
            Unit = unit
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.KeepAbove)]
    [InlineData(Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.KeepBelow)]
    [InlineData(Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.KeepBetween)]
    [InlineData(Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.Achieve)]
    [InlineData(Bud.Shared.Kernel.Enums.QuantitativeIndicatorType.Reduce)]
    public async Task Validate_QuantitativeWithAllValidTypes_PassesTypeValidation(Bud.Shared.Kernel.Enums.QuantitativeIndicatorType quantitativeType)
    {
        // Arrange
        var dto = new TemplateIndicatorRequest
        {
            Name = "Métrica",
            Type = Bud.Shared.Kernel.Enums.IndicatorType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = quantitativeType,
            MinValue = 10m,
            MaxValue = 100m,
            Unit = Bud.Shared.Kernel.Enums.IndicatorUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.Errors.Should().NotContain(e =>
            e.PropertyName.Contains("QuantitativeType") &&
            e.ErrorMessage.Contains("Tipo quantitativo inválido"));
    }

    #endregion
}
