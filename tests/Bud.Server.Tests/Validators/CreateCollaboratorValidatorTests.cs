using Bud.Server.Validators;
using Bud.Shared.Contracts;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Validators;

public class CreateCollaboratorValidatorTests
{
    private readonly CreateCollaboratorValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidData_ShouldPass()
    {
        // Arrange
        var request = new CreateCollaboratorRequest
        {
            FullName = "John Doe",
            Email = "john.doe@example.com",
            TeamId = Guid.NewGuid()
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
    public async Task Validate_WithEmptyFullName_ShouldFail(string? fullName)
    {
        // Arrange
        var request = new CreateCollaboratorRequest
        {
            FullName = fullName!,
            Email = "john.doe@example.com",
            TeamId = Guid.NewGuid()
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FullName");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Validate_WithEmptyEmail_ShouldFail(string? email)
    {
        // Arrange
        var request = new CreateCollaboratorRequest
        {
            FullName = "John Doe",
            Email = email!,
            TeamId = Guid.NewGuid()
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("invalid@")]
    [InlineData("@invalid")]
    [InlineData("invalid.com")]
    public async Task Validate_WithInvalidEmailFormat_ShouldFail(string email)
    {
        // Arrange
        var request = new CreateCollaboratorRequest
        {
            FullName = "John Doe",
            Email = email,
            TeamId = Guid.NewGuid()
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email" && e.ErrorMessage.Contains("valid email"));
    }

    [Fact]
    public async Task Validate_WithEmptyTeamId_ShouldFail()
    {
        // Arrange
        var request = new CreateCollaboratorRequest
        {
            FullName = "John Doe",
            Email = "john.doe@example.com",
            TeamId = Guid.Empty
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TeamId");
    }
}
