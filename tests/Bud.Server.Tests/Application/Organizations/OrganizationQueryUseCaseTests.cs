using Bud.Server.Application.Organizations;
using Bud.Server.Services;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Organizations;

public sealed class OrganizationQueryUseCaseTests
{
    [Fact]
    public async Task GetByIdAsync_DelegatesToService()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organizationService = new Mock<IOrganizationService>();
        organizationService
            .Setup(s => s.GetByIdAsync(organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<Organization>.Success(new Organization { Id = organizationId, Name = "Bud" }));

        var useCase = new OrganizationQueryUseCase(organizationService.Object);

        // Act
        var result = await useCase.GetByIdAsync(organizationId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(organizationId);
    }

    [Fact]
    public async Task GetAllAsync_DelegatesToService()
    {
        // Arrange
        var organizationService = new Mock<IOrganizationService>();
        organizationService
            .Setup(s => s.GetAllAsync("bud", 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<PagedResult<Organization>>.Success(new PagedResult<Organization>
            {
                Items = [],
                Total = 0,
                Page = 1,
                PageSize = 10
            }));

        var useCase = new OrganizationQueryUseCase(organizationService.Object);

        // Act
        var result = await useCase.GetAllAsync("bud", 1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        organizationService.Verify(s => s.GetAllAsync("bud", 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }
}
