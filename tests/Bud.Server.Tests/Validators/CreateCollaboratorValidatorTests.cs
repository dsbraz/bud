using Bud.Server.Domain.Repositories;
using Bud.Server.MultiTenancy;
using Bud.Server.Validators;
using Bud.Shared.Contracts;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Validators;

public sealed class CreateCollaboratorValidatorTests
{
    private readonly Mock<ICollaboratorRepository> _collaboratorRepository = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();
    private readonly CreateCollaboratorValidator _validator;

    public CreateCollaboratorValidatorTests()
    {
        _collaboratorRepository
            .Setup(x => x.IsEmailUniqueAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _collaboratorRepository
            .Setup(x => x.IsValidLeaderAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _tenantProvider
            .SetupGet(x => x.TenantId)
            .Returns(Guid.NewGuid());

        _validator = new CreateCollaboratorValidator(_collaboratorRepository.Object, _tenantProvider.Object);
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
        _collaboratorRepository
            .Setup(x => x.IsEmailUniqueAsync("existing@example.com", null, It.IsAny<CancellationToken>()))
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
        _collaboratorRepository
            .Setup(x => x.IsValidLeaderAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
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
