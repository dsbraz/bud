using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bud.Application.UnitTests.Application.Sessions;

public sealed class DeleteCurrentSessionTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsSuccess()
    {
        var useCase = new DeleteCurrentSession(NullLogger<DeleteCurrentSession>.Instance);

        var result = await useCase.ExecuteAsync();

        result.IsSuccess.Should().BeTrue();
    }
}
