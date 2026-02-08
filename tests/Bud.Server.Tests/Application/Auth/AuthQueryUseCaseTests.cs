using Bud.Server.Application.Auth;
using Bud.Server.Services;
using Bud.Shared.Contracts;
using FluentAssertions;
using Moq;
using Xunit;

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
            .ReturnsAsync(ServiceResult<List<OrganizationSummaryDto>>.Success([]));

        var useCase = new AuthQueryUseCase(authService.Object);

        // Act
        var result = await useCase.GetMyOrganizationsAsync(email);

        // Assert
        result.IsSuccess.Should().BeTrue();
        authService.Verify(s => s.GetMyOrganizationsAsync(email, It.IsAny<CancellationToken>()), Times.Once);
    }
}
