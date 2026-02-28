using Bud.Server.Validators;
using Bud.Shared.Contracts;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Validators;

public sealed class CreateObjectiveValidatorTests
{
    private readonly CreateGoalValidator _validator = new();

    private static CreateGoalRequest ValidChildGoalRequest(string name = "Objetivo 1") => new()
    {
        ParentId = Guid.NewGuid(),
        Name = name,
        Description = "Descrição",
        ScopeType = GoalScopeType.Organization,
        ScopeId = Guid.NewGuid(),
        StartDate = DateTime.UtcNow,
        EndDate = DateTime.UtcNow.AddDays(30),
        Status = GoalStatus.Planned
    };

    [Fact]
    public async Task Validate_WithValidRequest_Passes()
    {
        var request = ValidChildGoalRequest();

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_WithEmptyScopeId_Fails()
    {
        var request = new CreateGoalRequest
        {
            ParentId = Guid.NewGuid(),
            Name = "Objetivo 1",
            Description = "Descrição",
            ScopeType = GoalScopeType.Organization,
            ScopeId = Guid.Empty,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = GoalStatus.Planned
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("ScopeId") &&
            e.ErrorMessage.Contains("obrigatório"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Validate_WithEmptyName_Fails(string? name)
    {
        var request = ValidChildGoalRequest(name!);

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Name"));
    }

    [Fact]
    public async Task Validate_WithNameLongerThan200_Fails()
    {
        var request = ValidChildGoalRequest(new string('A', 201));

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("Name") &&
            e.ErrorMessage.Contains("200"));
    }

    [Fact]
    public async Task Validate_WithDescriptionLongerThan1000_Fails()
    {
        var request = new CreateGoalRequest
        {
            ParentId = Guid.NewGuid(),
            Name = "Objetivo 1",
            Description = new string('A', 1001),
            ScopeType = GoalScopeType.Organization,
            ScopeId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = GoalStatus.Planned
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("Description") &&
            e.ErrorMessage.Contains("1000"));
    }

    [Fact]
    public async Task Validate_WithNullDescription_Passes()
    {
        var request = new CreateGoalRequest
        {
            ParentId = Guid.NewGuid(),
            Name = "Objetivo 1",
            Description = null,
            ScopeType = GoalScopeType.Organization,
            ScopeId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = GoalStatus.Planned
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }
}
