using Bud.Server.Domain.Model;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Domain.Models;

public sealed class MissionScopeTests
{
    [Fact]
    public void TryCreate_ForOrganizationScope_ShouldIgnoreScopeId()
    {
        var success = GoalScope.TryCreate(GoalScopeType.Organization, Guid.Empty, out var scope);

        success.Should().BeTrue();
        scope.ScopeType.Should().Be(GoalScopeType.Organization);
        scope.ScopeId.Should().BeNull();
    }

    [Fact]
    public void TryCreate_ForTeamScope_WithEmptyId_ShouldFail()
    {
        var success = GoalScope.TryCreate(GoalScopeType.Team, Guid.Empty, out _);

        success.Should().BeFalse();
    }

    [Fact]
    public void Create_ForInvalidScope_ShouldThrow()
    {
        var act = () => GoalScope.Create(GoalScopeType.Workspace, Guid.Empty);

        act.Should().Throw<DomainInvariantException>();
    }
}
