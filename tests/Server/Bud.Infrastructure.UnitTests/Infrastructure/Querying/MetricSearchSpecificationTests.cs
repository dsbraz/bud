using Bud.Infrastructure.Querying;
using FluentAssertions;
using Xunit;

namespace Bud.Application.UnitTests.Application.Common.Specifications;

public sealed class MetricSearchSpecificationTests
{
    private static readonly Guid MissionId = Guid.NewGuid();

    private static readonly List<Indicator> Data =
    [
        new() { Id = Guid.NewGuid(), Name = "Receita Mensal", GoalId = MissionId },
        new() { Id = Guid.NewGuid(), Name = "NPS Score", GoalId = MissionId },
        new() { Id = Guid.NewGuid(), Name = "Taxa de Conversão", GoalId = MissionId }
    ];

    [Fact]
    public void Apply_WithSearchTerm_ShouldFilterByName()
    {
        var spec = new IndicatorSearchSpecification("receita", isNpgsql: false);

        var result = spec.Apply(Data.AsQueryable()).ToList();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Receita Mensal");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Apply_WithNullOrEmptySearch_ShouldReturnAll(string? search)
    {
        var spec = new IndicatorSearchSpecification(search, isNpgsql: false);

        var result = spec.Apply(Data.AsQueryable()).ToList();

        result.Should().HaveCount(3);
    }

    [Fact]
    public void Apply_WithNoMatch_ShouldReturnEmpty()
    {
        var spec = new IndicatorSearchSpecification("inexistente", isNpgsql: false);

        var result = spec.Apply(Data.AsQueryable()).ToList();

        result.Should().BeEmpty();
    }
}
