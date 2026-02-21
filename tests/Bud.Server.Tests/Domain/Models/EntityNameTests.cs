using Bud.Server.Domain.Model;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Domain.Models;

public sealed class EntityNameTests
{
    [Fact]
    public void TryCreate_WithValidName_ShouldNormalizeAndSucceed()
    {
        var result = EntityName.TryCreate("  Nome de Entidade  ", out var entityName);

        result.Should().BeTrue();
        entityName.Value.Should().Be("Nome de Entidade");
    }

    [Fact]
    public void TryCreate_WithWhitespace_ShouldFail()
    {
        var result = EntityName.TryCreate("   ", out _);

        result.Should().BeFalse();
    }

    [Fact]
    public void TryCreate_WithNameLongerThan200_ShouldFail()
    {
        var longName = new string('A', 201);

        var result = EntityName.TryCreate(longName, out _);

        result.Should().BeFalse();
    }
}
