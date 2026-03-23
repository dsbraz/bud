using Bud.Infrastructure.Querying;
using FluentAssertions;
using Xunit;

namespace Bud.Infrastructure.UnitTests.Infrastructure.Querying;

public sealed class TemplateSearchSpecificationTests
{
    private static readonly Guid OrgId = Guid.NewGuid();

    private static readonly List<Template> Data =
    [
        new() { Id = Guid.NewGuid(), Name = "Template OKR", OrganizationId = OrgId },
        new() { Id = Guid.NewGuid(), Name = "Template KPI", OrganizationId = OrgId },
        new() { Id = Guid.NewGuid(), Name = "Modelo Trimestral", OrganizationId = OrgId }
    ];

    [Fact]
    public void Apply_WithSearchTerm_ShouldFilterByName()
    {
        var spec = new TemplateSearchSpecification("OKR", isNpgsql: false);

        var result = spec.Apply(Data.AsQueryable()).ToList();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Template OKR");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Apply_WithNullOrEmptySearch_ShouldReturnAll(string? search)
    {
        var spec = new TemplateSearchSpecification(search, isNpgsql: false);

        var result = spec.Apply(Data.AsQueryable()).ToList();

        result.Should().HaveCount(3);
    }

    [Fact]
    public void Apply_WithNoMatch_ShouldReturnEmpty()
    {
        var spec = new TemplateSearchSpecification("inexistente", isNpgsql: false);

        var result = spec.Apply(Data.AsQueryable()).ToList();

        result.Should().BeEmpty();
    }
}
