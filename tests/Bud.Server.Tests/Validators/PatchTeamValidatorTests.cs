using Bud.Server.Validators;
using Bud.Shared.Contracts;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Validators;

public sealed class PatchTeamValidatorTests
{
    private readonly PatchTeamValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidRequest_Passes()
    {
        var request = new PatchTeamRequest
        {
            Name = "Time Atualizado",
            LeaderId = Guid.NewGuid()
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_WithNoFieldsSet_Passes()
    {
        var request = new PatchTeamRequest();

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
        var request = new PatchTeamRequest
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
        var request = new PatchTeamRequest
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

    #region LeaderId Validation

    [Fact]
    public async Task Validate_WithEmptyLeaderId_Fails()
    {
        var request = new PatchTeamRequest
        {
            LeaderId = Guid.Empty
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("LeaderId"));
    }

    #endregion
}
