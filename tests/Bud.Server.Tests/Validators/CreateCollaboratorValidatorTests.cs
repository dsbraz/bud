using Bud.Server.Services;
using Bud.Server.Validators;
using Bud.Shared.Contracts;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Validators;

public sealed class CreateCollaboratorValidatorTests
{
    private readonly Mock<ICollaboratorValidationService> _validationService = new();
    private readonly CreateCollaboratorValidator _validator;

    public CreateCollaboratorValidatorTests()
    {
        _validationService
            .Setup(x => x.IsEmailUniqueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _validationService
            .Setup(x => x.IsValidLeaderForCreateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _validator = new CreateCollaboratorValidator(_validationService.Object);
    }

    [Fact]
    public async Task Validate_WithValidData_ShouldPass()
    {
        var request = new CreateCollaboratorRequest
        {
            FullName = "John Doe",
            Email = "john.doe@example.com"
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Validate_WithEmptyFullName_ShouldFail(string? fullName)
    {
        var request = new CreateCollaboratorRequest
        {
            FullName = fullName!,
            Email = "john.doe@example.com"
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FullName");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Validate_WithEmptyEmail_ShouldFail(string? email)
    {
        var request = new CreateCollaboratorRequest
        {
            FullName = "John Doe",
            Email = email!
        };

        var result = await _validator.ValidateAsync(request);

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
        var request = new CreateCollaboratorRequest
        {
            FullName = "John Doe",
            Email = email
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email" && e.ErrorMessage.Contains("E-mail deve ser válido"));
    }

    [Fact]
    public async Task Validate_WithDuplicateEmail_ShouldFail()
    {
        _validationService
            .Setup(x => x.IsEmailUniqueAsync("existing@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new CreateCollaboratorRequest
        {
            FullName = "John Doe",
            Email = "existing@example.com"
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email" && e.ErrorMessage.Contains("E-mail já está em uso"));
    }

    [Fact]
    public async Task Validate_WithNonExistentLeader_ShouldFail()
    {
        _validationService
            .Setup(x => x.IsValidLeaderForCreateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new CreateCollaboratorRequest
        {
            FullName = "John Doe",
            Email = "john.doe@example.com",
            LeaderId = Guid.NewGuid()
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LeaderId");
    }
}
