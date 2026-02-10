using Bud.Server.Validators;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace Bud.Server.Tests.Validators;

#region CreateMissionTemplateValidator Tests

public sealed class CreateMissionTemplateValidatorTests
{
    private readonly CreateMissionTemplateValidator _validator = new();

    [Fact]
    public async Task Validate_ValidRequest_Passes()
    {
        // Arrange
        var request = new CreateMissionTemplateRequest
        {
            Name = "Template de Missão",
            Description = "Descrição do template",
            MissionNamePattern = "Missão {0}",
            MissionDescriptionPattern = "Descrição padrão",
            Metrics = []
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_ValidRequestWithMetrics_Passes()
    {
        // Arrange
        var request = new CreateMissionTemplateRequest
        {
            Name = "Template com Métricas",
            Metrics =
            [
                new MissionTemplateMetricDto
                {
                    Name = "Métrica Qualitativa",
                    Type = MetricType.Qualitative,
                    OrderIndex = 0,
                    TargetText = "Texto alvo"
                },
                new MissionTemplateMetricDto
                {
                    Name = "Métrica Quantitativa",
                    Type = MetricType.Quantitative,
                    OrderIndex = 1,
                    QuantitativeType = QuantitativeMetricType.Achieve,
                    MaxValue = 100m,
                    Unit = MetricUnit.Percentage
                }
            ]
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_MinimalValidRequest_Passes()
    {
        // Arrange
        var request = new CreateMissionTemplateRequest
        {
            Name = "Template Mínimo"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

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
        var request = new CreateMissionTemplateRequest
        {
            Name = name!
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Name" &&
            e.ErrorMessage.Contains("Nome é obrigatório"));
    }

    [Fact]
    public async Task Validate_NameExceeding200Chars_Fails()
    {
        // Arrange
        var request = new CreateMissionTemplateRequest
        {
            Name = new string('A', 201)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "Name" &&
            e.ErrorMessage.Contains("200"));
    }

    [Fact]
    public async Task Validate_NameExactly200Chars_Passes()
    {
        // Arrange
        var request = new CreateMissionTemplateRequest
        {
            Name = new string('A', 200)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_DescriptionExceeding1000Chars_Fails()
    {
        // Arrange
        var request = new CreateMissionTemplateRequest
        {
            Name = "Template",
            Description = new string('A', 1001)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "Description" &&
            e.ErrorMessage.Contains("1000"));
    }

    [Fact]
    public async Task Validate_DescriptionExactly1000Chars_Passes()
    {
        // Arrange
        var request = new CreateMissionTemplateRequest
        {
            Name = "Template",
            Description = new string('A', 1000)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_NullDescription_Passes()
    {
        // Arrange
        var request = new CreateMissionTemplateRequest
        {
            Name = "Template",
            Description = null
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_MissionNamePatternExceeding200Chars_Fails()
    {
        // Arrange
        var request = new CreateMissionTemplateRequest
        {
            Name = "Template",
            MissionNamePattern = new string('A', 201)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "MissionNamePattern" &&
            e.ErrorMessage.Contains("200"));
    }

    [Fact]
    public async Task Validate_MissionNamePatternExactly200Chars_Passes()
    {
        // Arrange
        var request = new CreateMissionTemplateRequest
        {
            Name = "Template",
            MissionNamePattern = new string('A', 200)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_NullMissionNamePattern_Passes()
    {
        // Arrange
        var request = new CreateMissionTemplateRequest
        {
            Name = "Template",
            MissionNamePattern = null
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_MissionDescriptionPatternExceeding1000Chars_Fails()
    {
        // Arrange
        var request = new CreateMissionTemplateRequest
        {
            Name = "Template",
            MissionDescriptionPattern = new string('A', 1001)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "MissionDescriptionPattern" &&
            e.ErrorMessage.Contains("1000"));
    }

    [Fact]
    public async Task Validate_MissionDescriptionPatternExactly1000Chars_Passes()
    {
        // Arrange
        var request = new CreateMissionTemplateRequest
        {
            Name = "Template",
            MissionDescriptionPattern = new string('A', 1000)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_NullMissionDescriptionPattern_Passes()
    {
        // Arrange
        var request = new CreateMissionTemplateRequest
        {
            Name = "Template",
            MissionDescriptionPattern = null
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_InvalidMetric_Fails()
    {
        // Arrange
        var request = new CreateMissionTemplateRequest
        {
            Name = "Template",
            Metrics =
            [
                new MissionTemplateMetricDto
                {
                    Name = "", // Invalid: empty name
                    Type = MetricType.Qualitative,
                    OrderIndex = 0,
                    TargetText = "Texto alvo"
                }
            ]
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName.Contains("Name") &&
            e.ErrorMessage.Contains("Nome é obrigatório"));
    }
}

#endregion

#region UpdateMissionTemplateValidator Tests

public sealed class UpdateMissionTemplateValidatorTests
{
    private readonly UpdateMissionTemplateValidator _validator = new();

    [Fact]
    public async Task Validate_ValidRequest_Passes()
    {
        // Arrange
        var request = new UpdateMissionTemplateRequest
        {
            Name = "Template Atualizado",
            Description = "Descrição atualizada",
            MissionNamePattern = "Missão {0}",
            MissionDescriptionPattern = "Descrição padrão",
            IsActive = true,
            Metrics = []
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_ValidRequestWithMetrics_Passes()
    {
        // Arrange
        var request = new UpdateMissionTemplateRequest
        {
            Name = "Template com Métricas",
            IsActive = true,
            Metrics =
            [
                new MissionTemplateMetricDto
                {
                    Name = "Métrica Qualitativa",
                    Type = MetricType.Qualitative,
                    OrderIndex = 0,
                    TargetText = "Texto alvo"
                }
            ]
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_InactiveTemplate_Passes()
    {
        // Arrange
        var request = new UpdateMissionTemplateRequest
        {
            Name = "Template Inativo",
            IsActive = false
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Validate_EmptyName_Fails(string? name)
    {
        // Arrange
        var request = new UpdateMissionTemplateRequest
        {
            Name = name!,
            IsActive = true
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Name" &&
            e.ErrorMessage.Contains("Nome é obrigatório"));
    }

    [Fact]
    public async Task Validate_NameExceeding200Chars_Fails()
    {
        // Arrange
        var request = new UpdateMissionTemplateRequest
        {
            Name = new string('A', 201),
            IsActive = true
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "Name" &&
            e.ErrorMessage.Contains("200"));
    }

    [Fact]
    public async Task Validate_DescriptionExceeding1000Chars_Fails()
    {
        // Arrange
        var request = new UpdateMissionTemplateRequest
        {
            Name = "Template",
            Description = new string('A', 1001),
            IsActive = true
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "Description" &&
            e.ErrorMessage.Contains("1000"));
    }

    [Fact]
    public async Task Validate_NullDescription_Passes()
    {
        // Arrange
        var request = new UpdateMissionTemplateRequest
        {
            Name = "Template",
            Description = null,
            IsActive = true
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_MissionNamePatternExceeding200Chars_Fails()
    {
        // Arrange
        var request = new UpdateMissionTemplateRequest
        {
            Name = "Template",
            MissionNamePattern = new string('A', 201),
            IsActive = true
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "MissionNamePattern" &&
            e.ErrorMessage.Contains("200"));
    }

    [Fact]
    public async Task Validate_NullMissionNamePattern_Passes()
    {
        // Arrange
        var request = new UpdateMissionTemplateRequest
        {
            Name = "Template",
            MissionNamePattern = null,
            IsActive = true
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_MissionDescriptionPatternExceeding1000Chars_Fails()
    {
        // Arrange
        var request = new UpdateMissionTemplateRequest
        {
            Name = "Template",
            MissionDescriptionPattern = new string('A', 1001),
            IsActive = true
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "MissionDescriptionPattern" &&
            e.ErrorMessage.Contains("1000"));
    }

    [Fact]
    public async Task Validate_NullMissionDescriptionPattern_Passes()
    {
        // Arrange
        var request = new UpdateMissionTemplateRequest
        {
            Name = "Template",
            MissionDescriptionPattern = null,
            IsActive = true
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_InvalidMetric_Fails()
    {
        // Arrange
        var request = new UpdateMissionTemplateRequest
        {
            Name = "Template",
            IsActive = true,
            Metrics =
            [
                new MissionTemplateMetricDto
                {
                    Name = "", // Invalid: empty name
                    Type = MetricType.Qualitative,
                    OrderIndex = 0,
                    TargetText = "Texto alvo"
                }
            ]
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName.Contains("Name") &&
            e.ErrorMessage.Contains("Nome é obrigatório"));
    }
}

#endregion

#region MissionTemplateMetricDtoValidator Tests

public sealed class MissionTemplateMetricDtoValidatorTests
{
    private readonly MissionTemplateMetricDtoValidator _validator = new();

    #region General Validation Tests

    [Fact]
    public async Task Validate_ValidQuantitativeMetric_Passes()
    {
        // Arrange
        var dto = new MissionTemplateMetricDto
        {
            Name = "Story Points",
            Type = MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = QuantitativeMetricType.KeepAbove,
            MinValue = 50m,
            Unit = MetricUnit.Points
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
        var dto = new MissionTemplateMetricDto
        {
            Name = "Qualidade do Código",
            Type = MetricType.Qualitative,
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
        var dto = new MissionTemplateMetricDto
        {
            Name = name!,
            Type = MetricType.Qualitative,
            OrderIndex = 0,
            TargetText = "Texto alvo"
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Name" &&
            e.ErrorMessage.Contains("Nome é obrigatório"));
    }

    [Fact]
    public async Task Validate_NameExceeding200Chars_Fails()
    {
        // Arrange
        var dto = new MissionTemplateMetricDto
        {
            Name = new string('A', 201),
            Type = MetricType.Qualitative,
            OrderIndex = 0,
            TargetText = "Texto alvo"
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "Name" &&
            e.ErrorMessage.Contains("200"));
    }

    [Fact]
    public async Task Validate_NameExactly200Chars_Passes()
    {
        // Arrange
        var dto = new MissionTemplateMetricDto
        {
            Name = new string('A', 200),
            Type = MetricType.Qualitative,
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
        var dto = new MissionTemplateMetricDto
        {
            Name = "Métrica",
            Type = (MetricType)99,
            OrderIndex = 0
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Type" &&
            e.ErrorMessage.Contains("Tipo inválido"));
    }

    [Fact]
    public async Task Validate_NegativeOrderIndex_Fails()
    {
        // Arrange
        var dto = new MissionTemplateMetricDto
        {
            Name = "Métrica",
            Type = MetricType.Qualitative,
            OrderIndex = -1,
            TargetText = "Texto alvo"
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "OrderIndex" &&
            e.ErrorMessage.Contains("maior ou igual a 0"));
    }

    [Fact]
    public async Task Validate_ZeroOrderIndex_Passes()
    {
        // Arrange
        var dto = new MissionTemplateMetricDto
        {
            Name = "Métrica",
            Type = MetricType.Qualitative,
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
    public async Task Validate_QualitativeWithoutTargetText_Fails()
    {
        // Arrange
        var dto = new MissionTemplateMetricDto
        {
            Name = "Métrica Qualitativa",
            Type = MetricType.Qualitative,
            OrderIndex = 0,
            TargetText = null
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

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
        var dto = new MissionTemplateMetricDto
        {
            Name = "Métrica Qualitativa",
            Type = MetricType.Qualitative,
            OrderIndex = 0,
            TargetText = ""
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

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
        var dto = new MissionTemplateMetricDto
        {
            Name = "Métrica Qualitativa",
            Type = MetricType.Qualitative,
            OrderIndex = 0,
            TargetText = new string('A', 1001)
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "TargetText" &&
            e.ErrorMessage.Contains("1000"));
    }

    [Fact]
    public async Task Validate_QualitativeWithTargetTextExactly1000Chars_Passes()
    {
        // Arrange
        var dto = new MissionTemplateMetricDto
        {
            Name = "Métrica Qualitativa",
            Type = MetricType.Qualitative,
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
        var dto = new MissionTemplateMetricDto
        {
            Name = "Métrica Quantitativa",
            Type = MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = null,
            Unit = MetricUnit.Points,
            MinValue = 50m
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "QuantitativeType" &&
            e.ErrorMessage.Contains("métricas quantitativas"));
    }

    [Fact]
    public async Task Validate_QuantitativeWithInvalidQuantitativeType_Fails()
    {
        // Arrange
        var dto = new MissionTemplateMetricDto
        {
            Name = "Métrica Quantitativa",
            Type = MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = (QuantitativeMetricType)99,
            Unit = MetricUnit.Points,
            MinValue = 50m
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "QuantitativeType" &&
            e.ErrorMessage.Contains("Tipo quantitativo inválido"));
    }

    [Fact]
    public async Task Validate_QuantitativeWithoutUnit_Fails()
    {
        // Arrange
        var dto = new MissionTemplateMetricDto
        {
            Name = "Métrica Quantitativa",
            Type = MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = QuantitativeMetricType.KeepAbove,
            Unit = null,
            MinValue = 50m
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Unit" &&
            e.ErrorMessage.Contains("métricas quantitativas"));
    }

    [Fact]
    public async Task Validate_QuantitativeWithInvalidUnit_Fails()
    {
        // Arrange
        var dto = new MissionTemplateMetricDto
        {
            Name = "Métrica Quantitativa",
            Type = MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = QuantitativeMetricType.KeepAbove,
            Unit = (MetricUnit)99,
            MinValue = 50m
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Unit" &&
            e.ErrorMessage.Contains("Unidade inválida"));
    }

    #endregion

    #region KeepAbove Tests

    [Fact]
    public async Task Validate_KeepAboveWithValidMinValue_Passes()
    {
        // Arrange
        var dto = new MissionTemplateMetricDto
        {
            Name = "Story Points",
            Type = MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = QuantitativeMetricType.KeepAbove,
            MinValue = 50m,
            Unit = MetricUnit.Points
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
        var dto = new MissionTemplateMetricDto
        {
            Name = "Story Points",
            Type = MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = QuantitativeMetricType.KeepAbove,
            MinValue = 0m,
            Unit = MetricUnit.Points
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
        var dto = new MissionTemplateMetricDto
        {
            Name = "Story Points",
            Type = MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = QuantitativeMetricType.KeepAbove,
            MinValue = null,
            Unit = MetricUnit.Points
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

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
        var dto = new MissionTemplateMetricDto
        {
            Name = "Story Points",
            Type = MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = QuantitativeMetricType.KeepAbove,
            MinValue = -10m,
            Unit = MetricUnit.Points
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "MinValue" &&
            e.ErrorMessage.Contains("maior ou igual a 0"));
    }

    #endregion

    #region KeepBelow Tests

    [Fact]
    public async Task Validate_KeepBelowWithValidMaxValue_Passes()
    {
        // Arrange
        var dto = new MissionTemplateMetricDto
        {
            Name = "Taxa de Erro",
            Type = MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = QuantitativeMetricType.KeepBelow,
            MaxValue = 5m,
            Unit = MetricUnit.Percentage
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
        var dto = new MissionTemplateMetricDto
        {
            Name = "Taxa de Erro",
            Type = MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = QuantitativeMetricType.KeepBelow,
            MaxValue = 0m,
            Unit = MetricUnit.Percentage
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
        var dto = new MissionTemplateMetricDto
        {
            Name = "Taxa de Erro",
            Type = MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = QuantitativeMetricType.KeepBelow,
            MaxValue = null,
            Unit = MetricUnit.Percentage
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

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
        var dto = new MissionTemplateMetricDto
        {
            Name = "Taxa de Erro",
            Type = MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = QuantitativeMetricType.KeepBelow,
            MaxValue = -5m,
            Unit = MetricUnit.Percentage
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

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
        var dto = new MissionTemplateMetricDto
        {
            Name = "Tempo de Resposta",
            Type = MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = QuantitativeMetricType.KeepBetween,
            MinValue = 100m,
            MaxValue = 500m,
            Unit = MetricUnit.Integer
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
        var dto = new MissionTemplateMetricDto
        {
            Name = "Tempo de Resposta",
            Type = MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = QuantitativeMetricType.KeepBetween,
            MinValue = null,
            MaxValue = 500m,
            Unit = MetricUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "MinValue" &&
            e.ErrorMessage.Contains("KeepBetween"));
    }

    [Fact]
    public async Task Validate_KeepBetweenWithoutMaxValue_Fails()
    {
        // Arrange
        var dto = new MissionTemplateMetricDto
        {
            Name = "Tempo de Resposta",
            Type = MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = QuantitativeMetricType.KeepBetween,
            MinValue = 100m,
            MaxValue = null,
            Unit = MetricUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "MaxValue" &&
            e.ErrorMessage.Contains("KeepBetween"));
    }

    [Fact]
    public async Task Validate_KeepBetweenWithMinValueGreaterThanMaxValue_Fails()
    {
        // Arrange
        var dto = new MissionTemplateMetricDto
        {
            Name = "Tempo de Resposta",
            Type = MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = QuantitativeMetricType.KeepBetween,
            MinValue = 500m,
            MaxValue = 100m,
            Unit = MetricUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "MaxValue" &&
            e.ErrorMessage.Contains("valor mínimo"));
    }

    [Fact]
    public async Task Validate_KeepBetweenWithEqualMinAndMaxValues_Fails()
    {
        // Arrange
        var dto = new MissionTemplateMetricDto
        {
            Name = "Tempo de Resposta",
            Type = MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = QuantitativeMetricType.KeepBetween,
            MinValue = 100m,
            MaxValue = 100m,
            Unit = MetricUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "MaxValue" &&
            e.ErrorMessage.Contains("valor mínimo"));
    }

    [Fact]
    public async Task Validate_KeepBetweenWithNegativeMinValue_Fails()
    {
        // Arrange
        var dto = new MissionTemplateMetricDto
        {
            Name = "Tempo de Resposta",
            Type = MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = QuantitativeMetricType.KeepBetween,
            MinValue = -10m,
            MaxValue = 100m,
            Unit = MetricUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "MinValue" &&
            e.ErrorMessage.Contains("maior ou igual a 0"));
    }

    [Fact]
    public async Task Validate_KeepBetweenWithNegativeMaxValue_Fails()
    {
        // Arrange
        var dto = new MissionTemplateMetricDto
        {
            Name = "Tempo de Resposta",
            Type = MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = QuantitativeMetricType.KeepBetween,
            MinValue = 0m,
            MaxValue = -5m,
            Unit = MetricUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "MaxValue" &&
            e.ErrorMessage.Contains("maior ou igual a 0"));
    }

    #endregion

    #region Achieve Tests

    [Fact]
    public async Task Validate_AchieveWithValidMaxValue_Passes()
    {
        // Arrange
        var dto = new MissionTemplateMetricDto
        {
            Name = "Meta de Vendas",
            Type = MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = QuantitativeMetricType.Achieve,
            MaxValue = 100m,
            Unit = MetricUnit.Integer
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
        var dto = new MissionTemplateMetricDto
        {
            Name = "Meta de Vendas",
            Type = MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = QuantitativeMetricType.Achieve,
            MaxValue = 0m,
            Unit = MetricUnit.Integer
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
        var dto = new MissionTemplateMetricDto
        {
            Name = "Meta de Vendas",
            Type = MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = QuantitativeMetricType.Achieve,
            MaxValue = null,
            Unit = MetricUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

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
        var dto = new MissionTemplateMetricDto
        {
            Name = "Meta de Vendas",
            Type = MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = QuantitativeMetricType.Achieve,
            MaxValue = -50m,
            Unit = MetricUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

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
        var dto = new MissionTemplateMetricDto
        {
            Name = "Redução de Custos",
            Type = MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = QuantitativeMetricType.Reduce,
            MaxValue = 50m,
            Unit = MetricUnit.Percentage
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
        var dto = new MissionTemplateMetricDto
        {
            Name = "Redução de Custos",
            Type = MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = QuantitativeMetricType.Reduce,
            MaxValue = 0m,
            Unit = MetricUnit.Percentage
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
        var dto = new MissionTemplateMetricDto
        {
            Name = "Redução de Custos",
            Type = MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = QuantitativeMetricType.Reduce,
            MaxValue = null,
            Unit = MetricUnit.Percentage
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

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
        var dto = new MissionTemplateMetricDto
        {
            Name = "Redução de Custos",
            Type = MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = QuantitativeMetricType.Reduce,
            MaxValue = -10m,
            Unit = MetricUnit.Percentage
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "MaxValue" &&
            e.ErrorMessage.Contains("maior ou igual a 0"));
    }

    #endregion

    #region Unit Enum Tests

    [Theory]
    [InlineData(MetricUnit.Integer)]
    [InlineData(MetricUnit.Decimal)]
    [InlineData(MetricUnit.Percentage)]
    [InlineData(MetricUnit.Hours)]
    [InlineData(MetricUnit.Points)]
    public async Task Validate_QuantitativeWithAllValidUnits_Passes(MetricUnit unit)
    {
        // Arrange
        var dto = new MissionTemplateMetricDto
        {
            Name = "Métrica",
            Type = MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = QuantitativeMetricType.Achieve,
            MaxValue = 100m,
            Unit = unit
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(QuantitativeMetricType.KeepAbove)]
    [InlineData(QuantitativeMetricType.KeepBelow)]
    [InlineData(QuantitativeMetricType.KeepBetween)]
    [InlineData(QuantitativeMetricType.Achieve)]
    [InlineData(QuantitativeMetricType.Reduce)]
    public async Task Validate_QuantitativeWithAllValidTypes_PassesTypeValidation(QuantitativeMetricType quantitativeType)
    {
        // Arrange
        var dto = new MissionTemplateMetricDto
        {
            Name = "Métrica",
            Type = MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = quantitativeType,
            MinValue = 10m,
            MaxValue = 100m,
            Unit = MetricUnit.Integer
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.Errors.Should().NotContain(e =>
            e.PropertyName == "QuantitativeType" &&
            e.ErrorMessage.Contains("Tipo quantitativo inválido"));
    }

    #endregion
}

#endregion
