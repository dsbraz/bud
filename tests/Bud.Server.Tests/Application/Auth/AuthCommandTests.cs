using Bud.Server.Infrastructure.Services;
using Bud.Server.Application.Auth;
using Bud.Server.Domain.ReadModels;
using Bud.Shared.Contracts;
using FluentAssertions;
using Moq;
using Xunit;
using Bud.Server.Application.Common;

namespace Bud.Server.Tests.Application.Auth;

public sealed class AuthCommandTests
{
    [Fact]
    public async Task LoginAsync_DelegatesToService()
    {
        // Arrange
        var request = new AuthLoginRequest { Email = "admin@getbud.co" };
        var authService = new Mock<IAuthService>();
        authService
            .Setup(s => s.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthLoginResult>.Success(new AuthLoginResult
            {
                Token = "token",
                Email = request.Email,
                DisplayName = "Administrador"
            }));

        var authCommand = new AuthCommand(authService.Object);

        // Act
        var result = await authCommand.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        authService.Verify(s => s.LoginAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }
}
