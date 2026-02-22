using Bud.Server.Validators;
using Bud.Shared.Contracts;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Validators;

public class TeamValidatorTests
{
    #region CreateTeamValidator Tests

    [Fact]
    public void CreateTeamValidator_WithValidRequest_PassesValidation()
    {
        var validator = new CreateTeamValidator();
        var request = new CreateTeamRequest
        {
            Name = "Test Team",
            WorkspaceId = Guid.NewGuid(),
            LeaderId = Guid.NewGuid()
        };

        var result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateTeamValidator_WithEmptyName_FailsValidation()
    {
        var validator = new CreateTeamValidator();
        var request = new CreateTeamRequest
        {
            Name = "",
            WorkspaceId = Guid.NewGuid(),
            LeaderId = Guid.NewGuid()
        };

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage == "Nome é obrigatório.");
    }

    [Fact]
    public void CreateTeamValidator_WithEmptyWorkspaceId_FailsValidation()
    {
        var validator = new CreateTeamValidator();
        var request = new CreateTeamRequest
        {
            Name = "Test Team",
            WorkspaceId = Guid.Empty,
            LeaderId = Guid.NewGuid()
        };

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "WorkspaceId" && e.ErrorMessage == "Workspace é obrigatório.");
    }

    [Fact]
    public void CreateTeamValidator_WithEmptyLeaderId_FailsValidation()
    {
        var validator = new CreateTeamValidator();
        var request = new CreateTeamRequest
        {
            Name = "Test Team",
            WorkspaceId = Guid.NewGuid(),
            LeaderId = Guid.Empty
        };

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LeaderId" && e.ErrorMessage == "Líder é obrigatório.");
    }

    [Fact]
    public void CreateTeamValidator_WithNameTooLong_FailsValidation()
    {
        var validator = new CreateTeamValidator();
        var request = new CreateTeamRequest
        {
            Name = new string('A', 201),
            WorkspaceId = Guid.NewGuid(),
            LeaderId = Guid.NewGuid()
        };

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage == "Nome deve ter no máximo 200 caracteres.");
    }

    #endregion

    #region PatchTeamValidator Tests

    [Fact]
    public void UpdateTeamValidator_WithValidRequest_PassesValidation()
    {
        var validator = new PatchTeamValidator();
        var request = new PatchTeamRequest
        {
            Name = "Test Team",
            LeaderId = Guid.NewGuid()
        };

        var result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateTeamValidator_WithEmptyName_FailsValidation()
    {
        var validator = new PatchTeamValidator();
        var request = new PatchTeamRequest
        {
            Name = "",
            LeaderId = Guid.NewGuid()
        };

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage == "Nome é obrigatório.");
    }

    [Fact]
    public void UpdateTeamValidator_WithEmptyLeaderId_FailsValidation()
    {
        var validator = new PatchTeamValidator();
        var request = new PatchTeamRequest
        {
            Name = "Test Team",
            LeaderId = Guid.Empty
        };

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LeaderId" && e.ErrorMessage == "Líder é obrigatório.");
    }

    #endregion
}
