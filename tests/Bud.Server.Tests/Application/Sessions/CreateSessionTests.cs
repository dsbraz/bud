using Bud.Server.Application.Ports;
using Bud.Server.Application.UseCases.Sessions;
using Bud.Server.Domain.ReadModels;
using Bud.Shared.Contracts;
using FluentAssertions;
using Moq;
using Xunit;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;

namespace Bud.Server.Tests.Application.Sessions;

public sealed class CreateSessionTests
{
    [Fact]
    public async Task ExecuteAsync_DelegatesToService()
    {
        // Arrange
        var request = new CreateSessionRequest { Email = "admin@getbud.co" };
        var authService = new Mock<IAuthService>();
        authService
            .Setup(s => s.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthLoginResult>.Success(new AuthLoginResult
            {
                Token = "token",
                Email = request.Email,
                DisplayName = "Administrador"
            }));

        var useCase = new CreateSession(authService.Object);

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        authService.Verify(s => s.LoginAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }
}
