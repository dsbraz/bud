using Bud.Server.Application.Auth;
using Bud.Server.Domain.ReadModels;
using Bud.Server.Application.Ports;
using Bud.Shared.Contracts;
using FluentAssertions;
using Moq;
using Xunit;
using Bud.Server.Application.Common;

namespace Bud.Server.Tests.Application.Auth;

public sealed class AuthQueryUseCaseTests
{
    [Fact]
    public async Task GetMyOrganizationsAsync_DelegatesToService()
    {
        // Arrange
        const string email = "admin@getbud.co";
        var authService = new Mock<IAuthService>();
        authService
            .Setup(s => s.GetMyOrganizationsAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<OrganizationSummary>>.Success([]));

        var useCase = new AuthQueryUseCase(authService.Object);

        // Act
        var result = await useCase.GetMyOrganizationsAsync(email);

        // Assert
        result.IsSuccess.Should().BeTrue();
        authService.Verify(s => s.GetMyOrganizationsAsync(email, It.IsAny<CancellationToken>()), Times.Once);
    }
}
