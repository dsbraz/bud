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
            Name = "Test Organization",
            OwnerId = Guid.NewGuid(),
            UserEmail = "admin@company.com"
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
            UserEmail = "admin@company.com"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WithNameExceeding200Characters_ShouldFail()
    {
        // Arrange
        var request = new CreateOrganizationRequest
        {
            Name = new string('A', 201),
            OwnerId = Guid.NewGuid(),
            UserEmail = "admin@company.com"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains("200"));
    }

    [Fact]
    public async Task Validate_WithNameExactly200Characters_ShouldPass()
    {
        // Arrange
        var request = new CreateOrganizationRequest
        {
            Name = new string('A', 200),
            OwnerId = Guid.NewGuid(),
            UserEmail = "admin@company.com"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyOwnerId_ShouldFail()
    {
        // Arrange
        var request = new CreateOrganizationRequest
        {
            Name = "Test Organization",
            OwnerId = Guid.Empty,
            UserEmail = "admin@company.com"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OwnerId");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Validate_WithEmptyOrWhitespaceUserEmail_ShouldFail(string? userEmail)
    {
        // Arrange
        var request = new CreateOrganizationRequest
        {
            Name = "Test Organization",
            OwnerId = Guid.NewGuid(),
            UserEmail = userEmail!
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserEmail");
    }
}
