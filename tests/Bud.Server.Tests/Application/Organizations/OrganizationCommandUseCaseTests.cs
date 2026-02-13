using Bud.Server.Application.Organizations;
using Bud.Server.Services;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Organizations;

public sealed class OrganizationCommandUseCaseTests
{
    [Fact]
    public async Task CreateAsync_DelegatesToService()
    {
        // Arrange
        var request = new CreateOrganizationRequest { Name = "Bud" };
        var organizationService = new Mock<IOrganizationService>();
        organizationService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<Organization>.Success(new Organization { Id = Guid.NewGuid(), Name = request.Name }));

        var useCase = new OrganizationCommandUseCase(organizationService.Object);

        // Act
        var result = await useCase.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        organizationService.Verify(s => s.CreateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_DelegatesToService()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organizationService = new Mock<IOrganizationService>();
        organizationService
            .Setup(s => s.DeleteAsync(organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Success());

        var useCase = new OrganizationCommandUseCase(organizationService.Object);

        // Act
        var result = await useCase.DeleteAsync(organizationId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        organizationService.Verify(s => s.DeleteAsync(organizationId, It.IsAny<CancellationToken>()), Times.Once);
    }

}
