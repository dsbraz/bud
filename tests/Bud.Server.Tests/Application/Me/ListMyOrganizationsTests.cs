using Bud.Server.Application.Ports;
using Bud.Server.Application.Me;
using Bud.Server.Domain.ReadModels;
using Bud.Shared.Contracts;
using FluentAssertions;
using Moq;
using Xunit;
using Bud.Server.Application.Common;

namespace Bud.Server.Tests.Application.Me;

public sealed class ListMyOrganizationsTests
{
    [Fact]
    public async Task ExecuteAsync_DelegatesToService()
    {
        // Arrange
        const string email = "admin@getbud.co";
        var authService = new Mock<IAuthService>();
        authService
            .Setup(s => s.GetMyOrganizationsAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<OrganizationSummary>>.Success([]));

        var useCase = new ListMyOrganizations(authService.Object);

        // Act
        var result = await useCase.ExecuteAsync(email);

        // Assert
        result.IsSuccess.Should().BeTrue();
        authService.Verify(s => s.GetMyOrganizationsAsync(email, It.IsAny<CancellationToken>()), Times.Once);
    }
}
