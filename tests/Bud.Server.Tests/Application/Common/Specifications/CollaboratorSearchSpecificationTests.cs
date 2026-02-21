using Bud.Server.Infrastructure.Querying;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Application.Common.Specifications;

public sealed class CollaboratorSearchSpecificationTests
{
    [Fact]
    public void Apply_WithSearchTerm_ShouldFilterByNameOrEmail()
    {
        var data = new List<Collaborator>
        {
            new() { Id = Guid.NewGuid(), FullName = "Ana Silva", Email = "ana@example.com", OrganizationId = Guid.NewGuid() },
            new() { Id = Guid.NewGuid(), FullName = "Bruno Costa", Email = "bruno@example.com", OrganizationId = Guid.NewGuid() }
        }.AsQueryable();

        var specification = new CollaboratorSearchSpecification("ANA", isNpgsql: false);

        var result = specification.Apply(data).ToList();

        result.Should().HaveCount(1);
        result[0].FullName.Should().Be("Ana Silva");
    }
}
