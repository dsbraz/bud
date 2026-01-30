using Bud.Server.Validators;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Validators;

public class WorkspaceValidatorTests
{
    private readonly CreateWorkspaceValidator _createValidator = new();
    private readonly UpdateWorkspaceValidator _updateValidator = new();

    #region CreateWorkspaceValidator Tests

    [Fact]
    public async Task CreateWorkspace_WithNullVisibility_ShouldFail()
    {
        var request = new CreateWorkspaceRequest
        {
            Name = "Test Workspace",
            OrganizationId = Guid.NewGuid(),
            Visibility = null
        };

        var result = await _createValidator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Visibility");
    }

    [Fact]
    public async Task CreateWorkspace_WithPublicVisibility_ShouldPass()
    {
        var request = new CreateWorkspaceRequest
        {
            Name = "Test Workspace",
            OrganizationId = Guid.NewGuid(),
            Visibility = Visibility.Public
        };

        var result = await _createValidator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CreateWorkspace_WithPrivateVisibility_ShouldPass()
    {
        var request = new CreateWorkspaceRequest
        {
            Name = "Test Workspace",
            OrganizationId = Guid.NewGuid(),
            Visibility = Visibility.Private
        };

        var result = await _createValidator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CreateWorkspace_WithInvalidVisibilityEnumValue_ShouldFail()
    {
        var request = new CreateWorkspaceRequest
        {
            Name = "Test Workspace",
            OrganizationId = Guid.NewGuid(),
            Visibility = (Visibility)99
        };

        var result = await _createValidator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Visibility");
    }

    #endregion

    #region UpdateWorkspaceValidator Tests

    [Fact]
    public async Task UpdateWorkspace_WithValidVisibility_ShouldPass()
    {
        var request = new UpdateWorkspaceRequest
        {
            Name = "Updated Name",
            Visibility = Visibility.Private
        };

        var result = await _updateValidator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateWorkspace_WithInvalidVisibilityEnumValue_ShouldFail()
    {
        var request = new UpdateWorkspaceRequest
        {
            Name = "Updated Name",
            Visibility = (Visibility)99
        };

        var result = await _updateValidator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Visibility");
    }

    #endregion
}
