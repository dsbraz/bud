using Bud.Server.Application.UseCases.Sessions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bud.Server.Tests.Application.Sessions;

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
