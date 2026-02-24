using Bud.Server.Validators;
using Bud.Shared.Contracts;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Validators;

public sealed class PatchMetricValidatorTests
{
    private readonly PatchMetricValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidRequest_Passes()
    {
        var request = new PatchMetricRequest
        {
            Name = "Métrica atualizada",
            Type = MetricType.Qualitative,
            TargetText = "Texto alvo"
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_WithNoFieldsSet_Passes()
    {
        var request = new PatchMetricRequest();

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    #region Name Validation

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Validate_WithEmptyName_Fails(string? name)
    {
        var request = new PatchMetricRequest
        {
            Name = name!
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Name"));
    }

    [Fact]
    public async Task Validate_WithNameExceeding200Characters_Fails()
    {
        var request = new PatchMetricRequest
        {
            Name = new string('A', 201)
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("Name") &&
            e.ErrorMessage.Contains("200"));
    }

    #endregion

    #region Type Validation

    [Fact]
    public async Task Validate_WithInvalidType_Fails()
    {
        var request = new PatchMetricRequest
        {
            Type = (MetricType)999
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName.Contains("Type") &&
            e.ErrorMessage.Contains("Tipo inválido"));
    }

    #endregion

    #region TargetText Validation

    [Fact]
    public async Task Validate_WithTargetTextExceeding1000Characters_Fails()
    {
        var request = new PatchMetricRequest
        {
            TargetText = new string('A', 1001)
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("TargetText") &&
            e.ErrorMessage.Contains("1000"));
    }

    #endregion

    #region QuantitativeType Validation

    [Fact]
    public async Task Validate_WithInvalidQuantitativeType_Fails()
    {
        var request = new PatchMetricRequest
        {
            QuantitativeType = new Optional<QuantitativeMetricType?>((QuantitativeMetricType)999)
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName.Contains("QuantitativeType") &&
            e.ErrorMessage.Contains("Tipo quantitativo inválido"));
    }

    #endregion

    #region Unit Validation

    [Fact]
    public async Task Validate_WithInvalidUnit_Fails()
    {
        var request = new PatchMetricRequest
        {
            Unit = new Optional<MetricUnit?>((MetricUnit)999)
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName.Contains("Unit") &&
            e.ErrorMessage.Contains("Unidade inválida"));
    }

    #endregion

    #region MinValue Validation

    [Fact]
    public async Task Validate_WithNegativeMinValue_Fails()
    {
        var request = new PatchMetricRequest
        {
            MinValue = new Optional<decimal?>(-10m)
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("MinValue") &&
            e.ErrorMessage.Contains("maior ou igual a 0"));
    }

    [Fact]
    public async Task Validate_WithZeroMinValue_Passes()
    {
        var request = new PatchMetricRequest
        {
            MinValue = new Optional<decimal?>(0m)
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region MaxValue Validation

    [Fact]
    public async Task Validate_WithNegativeMaxValue_Fails()
    {
        var request = new PatchMetricRequest
        {
            MaxValue = new Optional<decimal?>(-5m)
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("MaxValue") &&
            e.ErrorMessage.Contains("maior ou igual a 0"));
    }

    [Fact]
    public async Task Validate_WithZeroMaxValue_Passes()
    {
        var request = new PatchMetricRequest
        {
            MaxValue = new Optional<decimal?>(0m)
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    #endregion
}
