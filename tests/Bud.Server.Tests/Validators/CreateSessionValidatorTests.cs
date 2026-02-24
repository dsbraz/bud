using Bud.Server.Validators;
using Bud.Shared.Contracts;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Validators;

public sealed class CreateSessionValidatorTests
{
    private readonly CreateSessionValidator _validator = new();

    #region Valid Scenarios

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("nome@empresa.com.br")]
    [InlineData("admin")]
    [InlineData("Admin")]
    [InlineData("ADMIN")]
    [InlineData("admin@local")]
    [InlineData("admin@something.com")]
    public async Task Validate_WithValidEmail_Passes(string email)
    {
        var request = new CreateSessionRequest { Email = email };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    #endregion

    #region Invalid Scenarios

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Validate_WithEmptyEmail_Fails(string? email)
    {
        var request = new CreateSessionRequest { Email = email! };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Email"));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-at-sign")]
    [InlineData("@no-local-part.com")]
    public async Task Validate_WithInvalidEmail_Fails(string email)
    {
        var request = new CreateSessionRequest { Email = email };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName.Contains("Email") &&
            e.ErrorMessage.Contains("e-mail válido"));
    }

    #endregion
}
