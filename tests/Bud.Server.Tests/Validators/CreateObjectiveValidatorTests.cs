using Bud.Server.Validators;
using Bud.Shared.Contracts;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Validators;

public sealed class CreateObjectiveValidatorTests
{
    private readonly CreateObjectiveValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidRequest_Passes()
    {
        var request = new CreateObjectiveRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Objetivo 1",
            Description = "Descrição"
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_WithEmptyMissionId_Fails()
    {
        var request = new CreateObjectiveRequest
        {
            MissionId = Guid.Empty,
            Name = "Objetivo"
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("MissionId") &&
            e.ErrorMessage.Contains("obrigatória"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Validate_WithEmptyName_Fails(string? name)
    {
        var request = new CreateObjectiveRequest
        {
            MissionId = Guid.NewGuid(),
            Name = name!
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Name"));
    }

    [Fact]
    public async Task Validate_WithNameLongerThan200_Fails()
    {
        var request = new CreateObjectiveRequest
        {
            MissionId = Guid.NewGuid(),
            Name = new string('A', 201)
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("Name") &&
            e.ErrorMessage.Contains("200"));
    }

    [Fact]
    public async Task Validate_WithDescriptionLongerThan1000_Fails()
    {
        var request = new CreateObjectiveRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Objetivo",
            Description = new string('A', 1001)
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("Description") &&
            e.ErrorMessage.Contains("1000"));
    }

    [Fact]
    public async Task Validate_WithNullDescription_Passes()
    {
        var request = new CreateObjectiveRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Objetivo",
            Description = null
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }
}
