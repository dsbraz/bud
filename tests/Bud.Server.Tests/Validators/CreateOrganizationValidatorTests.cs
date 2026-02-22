using Bud.Server.Validators;
using Bud.Shared.Contracts;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Validators;

public class CreateOrganizationValidatorTests
{
    private readonly CreateOrganizationValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidRequest_ShouldPass()
    {
        // Arrange
        var request = new CreateOrganizationRequest
        {
            Name = "test.com",
            OwnerId = Guid.NewGuid(),
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
    public async Task Validate_WithEmptyOrWhitespaceName_ShouldFail(string? name)
    {
        // Arrange
        var request = new CreateOrganizationRequest
        {
            Name = name!,
            OwnerId = Guid.NewGuid(),
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Name"));
    }

    [Fact]
    public async Task Validate_WithNameExceeding200Characters_ShouldFail()
    {
        // Arrange — build a valid domain that exceeds 200 characters
        // "a{197}.com" = 197 + 1 + 3 = 201 chars
        var longLabel = new string('a', 197);
        var request = new CreateOrganizationRequest
        {
            Name = $"{longLabel}.com",
            OwnerId = Guid.NewGuid(),
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Name") && e.ErrorMessage.Contains("200"));
    }

    [Fact]
    public async Task Validate_WithDomainExactly200Characters_ShouldPass()
    {
        // Arrange — build a valid domain that is exactly 200 characters
        // "a{193}.com.br" = 193 + 1 + 3 + 1 + 2 = 200
        var label = new string('a', 193);
        var request = new CreateOrganizationRequest
        {
            Name = $"{label}.com.br",
            OwnerId = Guid.NewGuid(),
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("empresa.com.br")]
    [InlineData("my-company.org")]
    [InlineData("sub.domain.co.uk")]
    [InlineData("getbud.co")]
    [InlineData("example.com")]
    public async Task Validate_WithValidDomain_ShouldPass(string domain)
    {
        // Arrange
        var request = new CreateOrganizationRequest
        {
            Name = domain,
            OwnerId = Guid.NewGuid(),
        };

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
        var request = new CreateOrganizationRequest
        {
            Name = domain,
            OwnerId = Guid.NewGuid(),
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Name"));
    }

    [Fact]
    public async Task Validate_WithEmptyOwnerId_ShouldFail()
    {
        // Arrange
        var request = new CreateOrganizationRequest
        {
            Name = "test.com",
            OwnerId = Guid.Empty,
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("OwnerId"));
    }
}
