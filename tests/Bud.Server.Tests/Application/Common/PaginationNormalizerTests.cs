using Bud.Server.Application.Common;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Application.Common;

public sealed class PaginationNormalizerTests
{
    [Fact]
    public void Normalize_WhenPageIsLessThanOne_ShouldDefaultToOne()
    {
        // Act
        var (page, pageSize) = PaginationNormalizer.Normalize(page: 0, pageSize: 20);

        // Assert
        page.Should().Be(1);
        pageSize.Should().Be(20);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public void Normalize_WhenPageSizeIsOutsideRange_ShouldDefaultToTen(int inputPageSize)
    {
        // Act
        var (_, pageSize) = PaginationNormalizer.Normalize(page: 2, pageSize: inputPageSize);

        // Assert
        pageSize.Should().Be(10);
    }

    [Fact]
    public void Normalize_WhenValuesAreValid_ShouldKeepValues()
    {
        // Act
        var (page, pageSize) = PaginationNormalizer.Normalize(page: 3, pageSize: 50);

        // Assert
        page.Should().Be(3);
        pageSize.Should().Be(50);
    }
}
