using Bud.Server.Infrastructure.Querying;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Application.Common.Specifications;

public sealed class OrganizationSearchSpecificationTests
{
    [Fact]
    public void Apply_WithSearchTerm_ShouldFilterCaseInsensitive()
    {
        // Arrange
        var data = new List<Organization>
        {
            new() { Id = Guid.NewGuid(), Name = "ALPHA Corp" },
            new() { Id = Guid.NewGuid(), Name = "Beta Corp" }
        }.AsQueryable();

        var specification = new OrganizationSearchSpecification("alpha", isNpgsql: false);

        // Act
        var result = specification.Apply(data).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("ALPHA Corp");
    }
}
