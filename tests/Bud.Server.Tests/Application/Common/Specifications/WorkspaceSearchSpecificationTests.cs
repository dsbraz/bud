using Bud.Server.Domain.Common.Specifications;
using Bud.Shared.Models;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Application.Common.Specifications;

public sealed class WorkspaceSearchSpecificationTests
{
    [Fact]
    public void Apply_WithSearchTerm_ShouldFilterCaseInsensitive()
    {
        var data = new List<Workspace>
        {
            new() { Id = Guid.NewGuid(), Name = "Produto", OrganizationId = Guid.NewGuid() },
            new() { Id = Guid.NewGuid(), Name = "Marketing", OrganizationId = Guid.NewGuid() }
        }.AsQueryable();

        var specification = new WorkspaceSearchSpecification("prod", isNpgsql: false);

        var result = specification.Apply(data).ToList();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Produto");
    }
}
