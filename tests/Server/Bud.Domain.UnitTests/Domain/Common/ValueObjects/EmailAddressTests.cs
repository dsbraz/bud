using FluentAssertions;
using Xunit;

namespace Bud.Domain.UnitTests.Domain.Common.ValueObjects;

public sealed class EmailAddressTests
{
    [Fact]
    public void TryCreate_WithValidEmail_ShouldNormalize()
    {
        var success = EmailAddress.TryCreate("  USER@Example.COM ", out var email);

        success.Should().BeTrue();
        email.Value.Should().Be("user@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("invalid@")]
    [InlineData("@invalid.com")]
    public void TryCreate_WithInvalidEmail_ShouldFail(string email)
    {
        var success = EmailAddress.TryCreate(email, out _);

        success.Should().BeFalse();
    }

    [Fact]
    public void Create_WithValidEmail_ShouldSucceed()
    {
        var email = EmailAddress.Create("user@example.com");

        email.Value.Should().Be("user@example.com");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("invalid")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    public void Create_WithInvalidEmail_ShouldThrow(string? raw)
    {
        var act = () => EmailAddress.Create(raw);

        act.Should().Throw<DomainInvariantException>();
    }
}
