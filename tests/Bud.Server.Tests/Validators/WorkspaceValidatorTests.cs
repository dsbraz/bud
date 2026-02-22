using Bud.Server.Validators;
using Bud.Shared.Contracts;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Validators;

public class WorkspaceValidatorTests
{
    private readonly CreateWorkspaceValidator _createValidator = new();
    private readonly PatchWorkspaceValidator _updateValidator = new();

    #region CreateWorkspaceValidator Tests

    [Fact]
    public async Task CreateWorkspace_WithValidRequest_ShouldPass()
    {
        var request = new CreateWorkspaceRequest
        {
            Name = "Test Workspace",
            OrganizationId = Guid.NewGuid()
        };

        var result = await _createValidator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CreateWorkspace_WithEmptyName_ShouldFail()
    {
        var request = new CreateWorkspaceRequest
        {
            Name = "",
            OrganizationId = Guid.NewGuid()
        };

        var result = await _createValidator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task CreateWorkspace_WithEmptyOrganizationId_ShouldFail()
    {
        var request = new CreateWorkspaceRequest
        {
            Name = "Test Workspace",
            OrganizationId = Guid.Empty
        };

        var result = await _createValidator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrganizationId");
    }

    #endregion

    #region PatchWorkspaceValidator Tests

    [Fact]
    public async Task UpdateWorkspace_WithValidName_ShouldPass()
    {
        var request = new PatchWorkspaceRequest
        {
            Name = "Updated Name"
        };

        var result = await _updateValidator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateWorkspace_WithEmptyName_ShouldFail()
    {
        var request = new PatchWorkspaceRequest
        {
            Name = ""
        };

        var result = await _updateValidator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    #endregion
}
