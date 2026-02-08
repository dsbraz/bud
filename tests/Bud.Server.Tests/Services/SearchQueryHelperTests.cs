using Bud.Server.Services;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Services;

public sealed class SearchQueryHelperTests
{
    [Fact]
    public void ApplyCaseInsensitiveSearch_WhenSearchIsEmpty_ShouldReturnOriginalQuery()
    {
        // Arrange
        var data = new List<string> { "abc" }.AsQueryable();

        // Act
        var result = SearchQueryHelper.ApplyCaseInsensitiveSearch(
            data,
            "   ",
            isNpgsql: true,
            npgsqlFilter: (q, _) => q,
            fallbackFilter: (q, _) => q);

        // Assert
        result.Should().BeSameAs(data);
    }

    [Fact]
    public void ApplyCaseInsensitiveSearch_WhenNpgsql_ShouldUseEscapedPattern()
    {
        // Arrange
        var data = new List<string> { "abc" }.AsQueryable();
        string? received = null;

        // Act
        _ = SearchQueryHelper.ApplyCaseInsensitiveSearch(
            data,
            " a%b_c\\d ",
            isNpgsql: true,
            npgsqlFilter: (q, pattern) =>
            {
                received = pattern;
                return q;
            },
            fallbackFilter: (q, _) => q);

        // Assert
        received.Should().Be("%a\\%b\\_c\\\\d%");
    }

    [Fact]
    public void ApplyCaseInsensitiveSearch_WhenNotNpgsql_ShouldUseTrimmedTerm()
    {
        // Arrange
        var data = new List<string> { "abc" }.AsQueryable();
        string? received = null;

        // Act
        _ = SearchQueryHelper.ApplyCaseInsensitiveSearch(
            data,
            "  termo  ",
            isNpgsql: false,
            npgsqlFilter: (q, _) => q,
            fallbackFilter: (q, term) =>
            {
                received = term;
                return q;
            });

        // Assert
        received.Should().Be("termo");
    }
}
