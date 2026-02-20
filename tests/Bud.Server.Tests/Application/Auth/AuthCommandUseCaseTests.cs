using Bud.Server.Application.Auth;
using Bud.Server.Domain.ReadModels;
using Bud.Server.Services;
using Bud.Shared.Contracts;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Auth;

public sealed class AuthCommandUseCaseTests
{
    [Fact]
    public async Task LoginAsync_DelegatesToService()
    {
        // Arrange
        var request = new AuthLoginRequest { Email = "admin@getbud.co" };
        var authService = new Mock<IAuthService>();
        authService
            .Setup(s => s.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<AuthLoginResult>.Success(new AuthLoginResult
            {
                Token = "token",
                Email = request.Email,
                DisplayName = "Administrador"
            }));

        var useCase = new AuthCommandUseCase(authService.Object);

        // Act
        var result = await useCase.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        authService.Verify(s => s.LoginAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }
}
