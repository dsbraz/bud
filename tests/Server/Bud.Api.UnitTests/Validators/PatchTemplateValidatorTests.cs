using Bud.Api.Validators;
using Bud.Shared.Contracts;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace Bud.Api.UnitTests.Validators;

public sealed class PatchTemplateValidatorTests
{
    private readonly PatchTemplateValidator _validator = new();

    [Fact]
    public async Task Validate_ValidRequest_Passes()
    {
        // Arrange
        var request = new PatchTemplateRequest
        {
            Name = "Template Atualizado",
            Description = "Descrição atualizada",
            GoalNamePattern = "Missão {0}",
            GoalDescriptionPattern = "Descrição padrão",
            Indicators = new List<TemplateIndicatorRequest>()
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
            Indicators = new List<TemplateIndicatorRequest>
            {
                new()
                {
                    Name = "Métrica Qualitativa",
                    Type = Bud.Shared.Kernel.IndicatorType.Qualitative,
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
    public async Task Validate_GoalNamePatternExceeding200Chars_Fails()
    {
        // Arrange
        var request = new PatchTemplateRequest
        {
            Name = "Template",
            GoalNamePattern = new string('A', 201)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("GoalNamePattern") &&
            e.ErrorMessage.Contains("200"));
    }

    [Fact]
    public async Task Validate_NullGoalNamePattern_Passes()
    {
        // Arrange
        var request = new PatchTemplateRequest
        {
            Name = "Template",
            GoalNamePattern = null
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_GoalDescriptionPatternExceeding1000Chars_Fails()
    {
        // Arrange
        var request = new PatchTemplateRequest
        {
            Name = "Template",
            GoalDescriptionPattern = new string('A', 1001)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("GoalDescriptionPattern") &&
            e.ErrorMessage.Contains("1000"));
    }

    [Fact]
    public async Task Validate_NullGoalDescriptionPattern_Passes()
    {
        // Arrange
        var request = new PatchTemplateRequest
        {
            Name = "Template",
            GoalDescriptionPattern = null
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
            Indicators = new List<TemplateIndicatorRequest>
            {
                new()
                {
                    Name = "", // Invalid: empty name
                    Type = Bud.Shared.Kernel.IndicatorType.Qualitative,
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
            Goals = new List<TemplateGoalRequest>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Objetivo 1",
                    OrderIndex = 0
                }
            },
            Indicators = new List<TemplateIndicatorRequest>
            {
                new()
                {
                    Name = "Métrica 1",
                    Type = Bud.Shared.Kernel.IndicatorType.Qualitative,
                    OrderIndex = 0,
                    TemplateGoalId = Guid.NewGuid()
                }
            }
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("metas inexistentes"));
    }
}
