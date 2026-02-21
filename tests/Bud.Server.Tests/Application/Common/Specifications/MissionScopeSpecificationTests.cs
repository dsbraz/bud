using Bud.Server.Infrastructure.Querying;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Application.Common.Specifications;

public sealed class MissionScopeSpecificationTests
{
    [Fact]
    public void Apply_WithOrganizationScopeAndId_ShouldReturnOnlyOrganizationScopedMissions()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var otherOrgId = Guid.NewGuid();
        var data = new List<Mission>
        {
            new() { Id = Guid.NewGuid(), Name = "Org Scope", OrganizationId = orgId },
            new() { Id = Guid.NewGuid(), Name = "Workspace Scope", OrganizationId = orgId, WorkspaceId = Guid.NewGuid() },
            new() { Id = Guid.NewGuid(), Name = "Other Org", OrganizationId = otherOrgId }
        }.AsQueryable();

        var specification = new MissionScopeSpecification(MissionScopeType.Organization, orgId);

        // Act
        var result = specification.Apply(data).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Org Scope");
    }

    [Fact]
    public void Apply_WithTeamScopeWithoutId_ShouldReturnOnlyTeamScopedMissions()
    {
        // Arrange
        var data = new List<Mission>
        {
            new() { Id = Guid.NewGuid(), Name = "Org Scope", OrganizationId = Guid.NewGuid() },
            new() { Id = Guid.NewGuid(), Name = "Team Scope", OrganizationId = Guid.NewGuid(), TeamId = Guid.NewGuid() },
            new() { Id = Guid.NewGuid(), Name = "Collaborator Scope", OrganizationId = Guid.NewGuid(), CollaboratorId = Guid.NewGuid() }
        }.AsQueryable();

        var specification = new MissionScopeSpecification(MissionScopeType.Team, null);

        // Act
        var result = specification.Apply(data).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Team Scope");
    }
}
