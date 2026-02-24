using Bud.Server.Validators;
using Bud.Shared.Contracts;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace Bud.Server.Tests.Validators;

public sealed class CreateTemplateValidatorTests
{
    private readonly CreateTemplateValidator _validator = new();

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
