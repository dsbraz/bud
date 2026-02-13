using System.Reflection;
using Bud.Shared.Domain;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Architecture;

public sealed class AggregateRootArchitectureTests
{
    private static readonly Assembly SharedAssembly = typeof(ITenantEntity).Assembly;

    [Fact]
    public void AggregateRoots_ShouldImplementIAggregateRoot()
    {
        var expectedAggregateRoots = new[]
        {
            typeof(Organization),
            typeof(Workspace),
            typeof(Team),
            typeof(Collaborator),
            typeof(Mission),
            typeof(MissionTemplate)
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
            typeof(MissionMetric),
            typeof(MetricCheckin),
            typeof(MissionTemplateMetric),
            typeof(CollaboratorTeam),
            typeof(Notification),
            typeof(CollaboratorAccessLog)
        };

        var invalidRoots = nonRoots
            .Where(type => typeof(IAggregateRoot).IsAssignableFrom(type))
            .Select(type => type.FullName)
            .ToList();

        invalidRoots.Should().BeEmpty("entidades internas não devem ser marcadas como aggregate roots");
    }
}
