using Bud.Server.Validators;
using Bud.Shared.Contracts;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Validators;

public sealed class CreateObjectiveDimensionValidatorTests
{
    private readonly CreateObjectiveDimensionValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidRequest_Passes()
    {
        var result = await _validator.ValidateAsync(new CreateObjectiveDimensionRequest
        {
            Name = "Clientes"
        });

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Validate_WithEmptyName_Fails(string? name)
    {
        var result = await _validator.ValidateAsync(new CreateObjectiveDimensionRequest
        {
            Name = name!
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WithNameLongerThan100_Fails()
    {
        var result = await _validator.ValidateAsync(new CreateObjectiveDimensionRequest
        {
            Name = new string('A', 101)
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Name");
    }
}

public sealed class UpdateObjectiveDimensionValidatorTests
{
    private readonly UpdateObjectiveDimensionValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidRequest_Passes()
    {
        var result = await _validator.ValidateAsync(new UpdateObjectiveDimensionRequest
        {
            Name = "Processos"
        });

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithNameLongerThan100_Fails()
    {
        var result = await _validator.ValidateAsync(new UpdateObjectiveDimensionRequest
        {
            Name = new string('A', 101)
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Name");
    }
}
