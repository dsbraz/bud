using Bud.Server.Validators;
using Bud.Shared.Contracts;
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
        var request = new CreateTemplateRequest
        {
            Name = "Template de Missão",
            Description = "Descrição do template",
            MissionNamePattern = "Missão {0}",
            MissionDescriptionPattern = "Descrição padrão",
            Metrics = new List<TemplateMetricRequest>()
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
        var request = new CreateTemplateRequest
        {
            Name = "Template com Métricas",
            Metrics =
            [
                new TemplateMetricRequest
                {
                    Name = "Métrica Qualitativa",
                    Type = Bud.Shared.Contracts.MetricType.Qualitative,
                    OrderIndex = 0,
                    TargetText = "Texto alvo"
                },
                new TemplateMetricRequest
                {
                    Name = "Métrica Quantitativa",
                    Type = Bud.Shared.Contracts.MetricType.Quantitative,
                    OrderIndex = 1,
                    QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.Achieve,
                    MaxValue = 100m,
                    Unit = Bud.Shared.Contracts.MetricUnit.Percentage
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
        var request = new CreateTemplateRequest
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
        var request = new CreateTemplateRequest
        {
            Name = name!
        };

        // Act
        var result = await _validator.ValidateAsync(request);

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
        var request = new CreateTemplateRequest
        {
            Name = new string('A', 201)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

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
        var request = new CreateTemplateRequest
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
        var request = new CreateTemplateRequest
        {
            Name = "Template",
            Description = new string('A', 1001)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("Description") &&
            e.ErrorMessage.Contains("1000"));
    }

    [Fact]
    public async Task Validate_DescriptionExactly1000Chars_Passes()
    {
        // Arrange
        var request = new CreateTemplateRequest
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
        var request = new CreateTemplateRequest
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
        var request = new CreateTemplateRequest
        {
            Name = "Template",
            MissionNamePattern = new string('A', 201)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("MissionNamePattern") &&
            e.ErrorMessage.Contains("200"));
    }

    [Fact]
    public async Task Validate_MissionNamePatternExactly200Chars_Passes()
    {
        // Arrange
        var request = new CreateTemplateRequest
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
        var request = new CreateTemplateRequest
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
        var request = new CreateTemplateRequest
        {
            Name = "Template",
            MissionDescriptionPattern = new string('A', 1001)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("MissionDescriptionPattern") &&
            e.ErrorMessage.Contains("1000"));
    }

    [Fact]
    public async Task Validate_MissionDescriptionPatternExactly1000Chars_Passes()
    {
        // Arrange
        var request = new CreateTemplateRequest
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
        var request = new CreateTemplateRequest
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
        var request = new CreateTemplateRequest
        {
            Name = "Template",
            Metrics =
            [
                new TemplateMetricRequest
                {
                    Name = "", // Invalid: empty name
                    Type = Bud.Shared.Contracts.MetricType.Qualitative,
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

    [Fact]
    public async Task Validate_MetricReferencingUnknownObjective_Fails()
    {
        // Arrange
        var request = new CreateTemplateRequest
        {
            Name = "Template",
            Objectives =
            [
                new TemplateObjectiveRequest
                {
                    Id = Guid.NewGuid(),
                    Name = "Objetivo 1",
                    OrderIndex = 0
                }
            ],
            Metrics =
            [
                new TemplateMetricRequest
                {
                    Name = "Métrica 1",
                    Type = Bud.Shared.Contracts.MetricType.Qualitative,
                    OrderIndex = 0,
                    TemplateObjectiveId = Guid.NewGuid()
                }
            ]
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("objetivos inexistentes"));
    }
}

#endregion

#region PatchMissionTemplateValidator Tests

public sealed class UpdateMissionTemplateValidatorTests
{
    private readonly PatchMissionTemplateValidator _validator = new();

    [Fact]
    public async Task Validate_ValidRequest_Passes()
    {
        // Arrange
        var request = new PatchTemplateRequest
        {
            Name = "Template Atualizado",
            Description = "Descrição atualizada",
            MissionNamePattern = "Missão {0}",
            MissionDescriptionPattern = "Descrição padrão",
            Metrics = new List<TemplateMetricRequest>()
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
        var request = new PatchTemplateRequest
        {
            Name = "Template com Métricas",
            Metrics = new List<TemplateMetricRequest>
            {
                new()
                {
                    Name = "Métrica Qualitativa",
                    Type = Bud.Shared.Contracts.MetricType.Qualitative,
                    OrderIndex = 0,
                    TargetText = "Texto alvo"
                }
            }
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
        var request = new PatchTemplateRequest
        {
            Name = name!
        };

        // Act
        var result = await _validator.ValidateAsync(request);

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
        var request = new PatchTemplateRequest
        {
            Name = new string('A', 201)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("Name") &&
            e.ErrorMessage.Contains("200"));
    }

    [Fact]
    public async Task Validate_DescriptionExceeding1000Chars_Fails()
    {
        // Arrange
        var request = new PatchTemplateRequest
        {
            Name = "Template",
            Description = new string('A', 1001)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("Description") &&
            e.ErrorMessage.Contains("1000"));
    }

    [Fact]
    public async Task Validate_NullDescription_Passes()
    {
        // Arrange
        var request = new PatchTemplateRequest
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
        var request = new PatchTemplateRequest
        {
            Name = "Template",
            MissionNamePattern = new string('A', 201)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("MissionNamePattern") &&
            e.ErrorMessage.Contains("200"));
    }

    [Fact]
    public async Task Validate_NullMissionNamePattern_Passes()
    {
        // Arrange
        var request = new PatchTemplateRequest
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
        var request = new PatchTemplateRequest
        {
            Name = "Template",
            MissionDescriptionPattern = new string('A', 1001)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("MissionDescriptionPattern") &&
            e.ErrorMessage.Contains("1000"));
    }

    [Fact]
    public async Task Validate_NullMissionDescriptionPattern_Passes()
    {
        // Arrange
        var request = new PatchTemplateRequest
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
        var request = new PatchTemplateRequest
        {
            Name = "Template",
            Metrics = new List<TemplateMetricRequest>
            {
                new()
                {
                    Name = "", // Invalid: empty name
                    Type = Bud.Shared.Contracts.MetricType.Qualitative,
                    OrderIndex = 0,
                    TargetText = "Texto alvo"
                }
            }
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName.Contains("Name") &&
            e.ErrorMessage.Contains("Nome é obrigatório"));
    }

    [Fact]
    public async Task Validate_MetricReferencingUnknownObjective_Fails()
    {
        // Arrange
        var request = new PatchTemplateRequest
        {
            Name = "Template",
            Objectives = new List<TemplateObjectiveRequest>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Objetivo 1",
                    OrderIndex = 0
                }
            },
            Metrics = new List<TemplateMetricRequest>
            {
                new()
                {
                    Name = "Métrica 1",
                    Type = Bud.Shared.Contracts.MetricType.Qualitative,
                    OrderIndex = 0,
                    TemplateObjectiveId = Guid.NewGuid()
                }
            }
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("objetivos inexistentes"));
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
        var dto = new TemplateMetricRequest
        {
            Name = "Story Points",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.KeepAbove,
            MinValue = 50m,
            Unit = Bud.Shared.Contracts.MetricUnit.Points
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
        var dto = new TemplateMetricRequest
        {
            Name = "Qualidade do Código",
            Type = Bud.Shared.Contracts.MetricType.Qualitative,
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
        var dto = new TemplateMetricRequest
        {
            Name = name!,
            Type = Bud.Shared.Contracts.MetricType.Qualitative,
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
        var dto = new TemplateMetricRequest
        {
            Name = new string('A', 201),
            Type = Bud.Shared.Contracts.MetricType.Qualitative,
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
        var dto = new TemplateMetricRequest
        {
            Name = new string('A', 200),
            Type = Bud.Shared.Contracts.MetricType.Qualitative,
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
        var dto = new TemplateMetricRequest
        {
            Name = "Métrica",
            Type = (Bud.Shared.Contracts.MetricType)99,
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
        var dto = new TemplateMetricRequest
        {
            Name = "Métrica",
            Type = Bud.Shared.Contracts.MetricType.Qualitative,
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
        var dto = new TemplateMetricRequest
        {
            Name = "Métrica",
            Type = Bud.Shared.Contracts.MetricType.Qualitative,
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
        var dto = new TemplateMetricRequest
        {
            Name = "Métrica Qualitativa",
            Type = Bud.Shared.Contracts.MetricType.Qualitative,
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
        var dto = new TemplateMetricRequest
        {
            Name = "Métrica Qualitativa",
            Type = Bud.Shared.Contracts.MetricType.Qualitative,
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
        var dto = new TemplateMetricRequest
        {
            Name = "Métrica Qualitativa",
            Type = Bud.Shared.Contracts.MetricType.Qualitative,
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
        var dto = new TemplateMetricRequest
        {
            Name = "Métrica Qualitativa",
            Type = Bud.Shared.Contracts.MetricType.Qualitative,
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
        var dto = new TemplateMetricRequest
        {
            Name = "Métrica Quantitativa",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = null,
            Unit = Bud.Shared.Contracts.MetricUnit.Points,
            MinValue = 50m
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName.Contains("QuantitativeType") &&
            e.ErrorMessage.Contains("métricas quantitativas"));
    }

    [Fact]
    public async Task Validate_QuantitativeWithInvalidQuantitativeType_Fails()
    {
        // Arrange
        var dto = new TemplateMetricRequest
        {
            Name = "Métrica Quantitativa",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = (Bud.Shared.Contracts.QuantitativeMetricType)99,
            Unit = Bud.Shared.Contracts.MetricUnit.Points,
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
        var dto = new TemplateMetricRequest
        {
            Name = "Métrica Quantitativa",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.KeepAbove,
            Unit = null,
            MinValue = 50m
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName.Contains("Unit") &&
            e.ErrorMessage.Contains("métricas quantitativas"));
    }

    [Fact]
    public async Task Validate_QuantitativeWithInvalidUnit_Fails()
    {
        // Arrange
        var dto = new TemplateMetricRequest
        {
            Name = "Métrica Quantitativa",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.KeepAbove,
            Unit = (Bud.Shared.Contracts.MetricUnit)99,
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
        var dto = new TemplateMetricRequest
        {
            Name = "Story Points",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.KeepAbove,
            MinValue = 50m,
            Unit = Bud.Shared.Contracts.MetricUnit.Points
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
        var dto = new TemplateMetricRequest
        {
            Name = "Story Points",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.KeepAbove,
            MinValue = 0m,
            Unit = Bud.Shared.Contracts.MetricUnit.Points
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
        var dto = new TemplateMetricRequest
        {
            Name = "Story Points",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.KeepAbove,
            MinValue = null,
            Unit = Bud.Shared.Contracts.MetricUnit.Points
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
        var dto = new TemplateMetricRequest
        {
            Name = "Story Points",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.KeepAbove,
            MinValue = -10m,
            Unit = Bud.Shared.Contracts.MetricUnit.Points
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
        var dto = new TemplateMetricRequest
        {
            Name = "Taxa de Erro",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.KeepBelow,
            MaxValue = 5m,
            Unit = Bud.Shared.Contracts.MetricUnit.Percentage
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
        var dto = new TemplateMetricRequest
        {
            Name = "Taxa de Erro",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.KeepBelow,
            MaxValue = 0m,
            Unit = Bud.Shared.Contracts.MetricUnit.Percentage
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
        var dto = new TemplateMetricRequest
        {
            Name = "Taxa de Erro",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.KeepBelow,
            MaxValue = null,
            Unit = Bud.Shared.Contracts.MetricUnit.Percentage
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
        var dto = new TemplateMetricRequest
        {
            Name = "Taxa de Erro",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.KeepBelow,
            MaxValue = -5m,
            Unit = Bud.Shared.Contracts.MetricUnit.Percentage
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
        var dto = new TemplateMetricRequest
        {
            Name = "Tempo de Resposta",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.KeepBetween,
            MinValue = 100m,
            MaxValue = 500m,
            Unit = Bud.Shared.Contracts.MetricUnit.Integer
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
        var dto = new TemplateMetricRequest
        {
            Name = "Tempo de Resposta",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.KeepBetween,
            MinValue = null,
            MaxValue = 500m,
            Unit = Bud.Shared.Contracts.MetricUnit.Integer
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
        var dto = new TemplateMetricRequest
        {
            Name = "Tempo de Resposta",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.KeepBetween,
            MinValue = 100m,
            MaxValue = null,
            Unit = Bud.Shared.Contracts.MetricUnit.Integer
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
        var dto = new TemplateMetricRequest
        {
            Name = "Tempo de Resposta",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.KeepBetween,
            MinValue = 500m,
            MaxValue = 100m,
            Unit = Bud.Shared.Contracts.MetricUnit.Integer
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
        var dto = new TemplateMetricRequest
        {
            Name = "Tempo de Resposta",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.KeepBetween,
            MinValue = 100m,
            MaxValue = 100m,
            Unit = Bud.Shared.Contracts.MetricUnit.Integer
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
        var dto = new TemplateMetricRequest
        {
            Name = "Tempo de Resposta",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.KeepBetween,
            MinValue = -10m,
            MaxValue = 100m,
            Unit = Bud.Shared.Contracts.MetricUnit.Integer
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
        var dto = new TemplateMetricRequest
        {
            Name = "Tempo de Resposta",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.KeepBetween,
            MinValue = 0m,
            MaxValue = -5m,
            Unit = Bud.Shared.Contracts.MetricUnit.Integer
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
        var dto = new TemplateMetricRequest
        {
            Name = "Meta de Vendas",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.Achieve,
            MaxValue = 100m,
            Unit = Bud.Shared.Contracts.MetricUnit.Integer
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
        var dto = new TemplateMetricRequest
        {
            Name = "Meta de Vendas",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.Achieve,
            MaxValue = 0m,
            Unit = Bud.Shared.Contracts.MetricUnit.Integer
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
        var dto = new TemplateMetricRequest
        {
            Name = "Meta de Vendas",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.Achieve,
            MaxValue = null,
            Unit = Bud.Shared.Contracts.MetricUnit.Integer
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
        var dto = new TemplateMetricRequest
        {
            Name = "Meta de Vendas",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.Achieve,
            MaxValue = -50m,
            Unit = Bud.Shared.Contracts.MetricUnit.Integer
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
        var dto = new TemplateMetricRequest
        {
            Name = "Redução de Custos",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.Reduce,
            MaxValue = 50m,
            Unit = Bud.Shared.Contracts.MetricUnit.Percentage
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
        var dto = new TemplateMetricRequest
        {
            Name = "Redução de Custos",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.Reduce,
            MaxValue = 0m,
            Unit = Bud.Shared.Contracts.MetricUnit.Percentage
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
        var dto = new TemplateMetricRequest
        {
            Name = "Redução de Custos",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.Reduce,
            MaxValue = null,
            Unit = Bud.Shared.Contracts.MetricUnit.Percentage
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
        var dto = new TemplateMetricRequest
        {
            Name = "Redução de Custos",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.Reduce,
            MaxValue = -10m,
            Unit = Bud.Shared.Contracts.MetricUnit.Percentage
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
    [InlineData(Bud.Shared.Contracts.MetricUnit.Integer)]
    [InlineData(Bud.Shared.Contracts.MetricUnit.Decimal)]
    [InlineData(Bud.Shared.Contracts.MetricUnit.Percentage)]
    [InlineData(Bud.Shared.Contracts.MetricUnit.Hours)]
    [InlineData(Bud.Shared.Contracts.MetricUnit.Points)]
    public async Task Validate_QuantitativeWithAllValidUnits_Passes(Bud.Shared.Contracts.MetricUnit unit)
    {
        // Arrange
        var dto = new TemplateMetricRequest
        {
            Name = "Métrica",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.Achieve,
            MaxValue = 100m,
            Unit = unit
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(Bud.Shared.Contracts.QuantitativeMetricType.KeepAbove)]
    [InlineData(Bud.Shared.Contracts.QuantitativeMetricType.KeepBelow)]
    [InlineData(Bud.Shared.Contracts.QuantitativeMetricType.KeepBetween)]
    [InlineData(Bud.Shared.Contracts.QuantitativeMetricType.Achieve)]
    [InlineData(Bud.Shared.Contracts.QuantitativeMetricType.Reduce)]
    public async Task Validate_QuantitativeWithAllValidTypes_PassesTypeValidation(Bud.Shared.Contracts.QuantitativeMetricType quantitativeType)
    {
        // Arrange
        var dto = new TemplateMetricRequest
        {
            Name = "Métrica",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            OrderIndex = 0,
            QuantitativeType = quantitativeType,
            MinValue = 10m,
            MaxValue = 100m,
            Unit = Bud.Shared.Contracts.MetricUnit.Integer
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

#endregion
