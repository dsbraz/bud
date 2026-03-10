using FluentAssertions;
using Xunit;

namespace Bud.ArchitectureTests.Architecture;

public sealed class AggregateRootArchitectureTests
{
    [Fact]
    public void AggregateRoots_ShouldImplementIAggregateRoot()
    {
        var expectedAggregateRoots = new[]
        {
            typeof(Organization),
            typeof(Workspace),
            typeof(Team),
            typeof(Collaborator),
            typeof(Goal),
            typeof(Indicator),
            typeof(Template),
            typeof(Notification)
        };

        var missingMarker = expectedAggregateRoots
            .Where(type => !typeof(IAggregateRoot).IsAssignableFrom(type))
            .Select(type => type.FullName)
            .ToList();

        missingMarker.Should().BeEmpty("aggregate roots devem ser explícitas para reforçar boundaries de domínio");
    }

    [Fact]
    public void ChildEntities_ShouldNotImplementIAggregateRoot()
    {
        var nonRoots = new[]
        {
            typeof(Checkin),
            typeof(TemplateGoal),
            typeof(TemplateIndicator),
            typeof(CollaboratorTeam),
            typeof(CollaboratorAccessLog)
        };

        var invalidRoots = nonRoots
            .Where(type => typeof(IAggregateRoot).IsAssignableFrom(type))
            .Select(type => type.FullName)
            .ToList();

        invalidRoots.Should().BeEmpty("entidades internas não devem ser marcadas como aggregate roots");
    }
}
