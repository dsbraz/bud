using Bud.Server.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Domain.Common.ValueObjects;

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
}
