using Bud.Server.Application.UseCases.Sessions;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Application.Sessions;

public sealed class DeleteCurrentSessionTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsSuccess()
    {
        var useCase = new DeleteCurrentSession();

        var result = await useCase.ExecuteAsync();

        result.IsSuccess.Should().BeTrue();
    }
}
