using Bud.Server.Infrastructure.Querying;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Application.Common.Specifications;

public sealed class TeamSearchSpecificationTests
{
    [Fact]
    public void Apply_WithSearchTerm_ShouldFilterByTeamName()
    {
        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "Core", OrganizationId = Guid.NewGuid() };
        var data = new List<Team>
        {
            new() { Id = Guid.NewGuid(), Name = "Plataforma", Workspace = workspace, WorkspaceId = workspace.Id, OrganizationId = workspace.OrganizationId },
            new() { Id = Guid.NewGuid(), Name = "Vendas", Workspace = workspace, WorkspaceId = workspace.Id, OrganizationId = workspace.OrganizationId }
        }.AsQueryable();

        var specification = new TeamSearchSpecification("plata", isNpgsql: false);

        var result = specification.Apply(data).ToList();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Plataforma");
    }
}
