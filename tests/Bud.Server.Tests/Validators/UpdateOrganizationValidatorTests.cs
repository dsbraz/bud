using Bud.Server.Validators;
using Bud.Shared.Contracts;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Validators;

public class UpdateOrganizationValidatorTests
{
    private readonly PatchOrganizationValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidDomain_ShouldPass()
    {
        // Arrange
        var request = new PatchOrganizationRequest { Name = "empresa.com.br" };

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
    public async Task Validate_WithEmptyOrWhitespaceName_ShouldFail(string? name)
    {
        // Arrange
        var request = new PatchOrganizationRequest { Name = name! };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Name"));
    }

    [Fact]
    public async Task Validate_WithNameExceeding200Characters_ShouldFail()
    {
        // Arrange — "a{197}.com" = 197 + 1 + 3 = 201 chars
        var longLabel = new string('a', 197);
        var request = new PatchOrganizationRequest { Name = $"{longLabel}.com" };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Name") && e.ErrorMessage.Contains("200"));
    }

    [Theory]
    [InlineData("empresa.com.br")]
    [InlineData("my-company.org")]
    [InlineData("sub.domain.co.uk")]
    [InlineData("getbud.co")]
    [InlineData("example.com")]
    public async Task Validate_WithValidDomains_ShouldPass(string domain)
    {
        // Arrange
        var request = new PatchOrganizationRequest { Name = domain };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("just text")]
    [InlineData("no-dots")]
    [InlineData("-invalid.com")]
    [InlineData("invalid-.com")]
    [InlineData(".leading-dot.com")]
    [InlineData("trailing-dot.com.")]
    [InlineData("spaces in.com")]
    [InlineData("under_score.com")]
    public async Task Validate_WithInvalidDomain_ShouldFail(string domain)
    {
        // Arrange
        var request = new PatchOrganizationRequest { Name = domain };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Name"));
    }
}
