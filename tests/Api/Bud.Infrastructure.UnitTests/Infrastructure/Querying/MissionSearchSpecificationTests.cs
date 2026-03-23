using Bud.Infrastructure.Querying;
using FluentAssertions;
using Xunit;

namespace Bud.Infrastructure.UnitTests.Infrastructure.Querying;

public sealed class MissionSearchSpecificationTests
{
    private static readonly Guid OrgId = Guid.NewGuid();

    private static readonly List<Goal> Data =
    [
        new() { Id = Guid.NewGuid(), Name = "Missão Alpha", OrganizationId = OrgId },
        new() { Id = Guid.NewGuid(), Name = "Missão Beta", OrganizationId = OrgId },
        new() { Id = Guid.NewGuid(), Name = "Projeto Gamma", OrganizationId = OrgId }
    ];

    [Fact]
    public void Apply_WithSearchTerm_ShouldFilterByName()
    {
        var spec = new GoalSearchSpecification("ALPHA", isNpgsql: false);

        var result = spec.Apply(Data.AsQueryable()).ToList();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Missão Alpha");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Apply_WithNullOrEmptySearch_ShouldReturnAll(string? search)
    {
        var spec = new GoalSearchSpecification(search, isNpgsql: false);

        var result = spec.Apply(Data.AsQueryable()).ToList();

        result.Should().HaveCount(3);
    }

    [Fact]
    public void Apply_WithNoMatch_ShouldReturnEmpty()
    {
        var spec = new GoalSearchSpecification("inexistente", isNpgsql: false);

        var result = spec.Apply(Data.AsQueryable()).ToList();

        result.Should().BeEmpty();
    }
}
