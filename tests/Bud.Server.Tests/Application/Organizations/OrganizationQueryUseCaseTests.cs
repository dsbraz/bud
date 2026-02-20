using Bud.Server.Infrastructure.Repositories;
using Bud.Server.Application.Common;
using Bud.Server.Application.Organizations;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Organizations;

public sealed class OrganizationQueryUseCaseTests
{
    private readonly Mock<IOrganizationRepository> _orgRepo = new();

    [Fact]
    public async Task GetByIdAsync_WithExistingOrganization_ReturnsSuccess()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        _orgRepo.Setup(r => r.GetByIdAsync(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Organization { Id = orgId, Name = "Bud" });

        var useCase = new OrganizationQueryUseCase(_orgRepo.Object);

        // Act
        var result = await useCase.GetByIdAsync(orgId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(orgId);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        _orgRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Organization?)null);

        var useCase = new OrganizationQueryUseCase(_orgRepo.Object);

        // Act
        var result = await useCase.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Organização não encontrada.");
    }

    [Fact]
    public async Task GetAllAsync_DelegatesToRepository()
    {
        // Arrange
        _orgRepo.Setup(r => r.GetAllAsync("bud", 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Organization>
            {
                Items = [],
                Total = 0,
                Page = 1,
                PageSize = 10
            });

        var useCase = new OrganizationQueryUseCase(_orgRepo.Object);

        // Act
        var result = await useCase.GetAllAsync("bud", 1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _orgRepo.Verify(r => r.GetAllAsync("bud", 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetWorkspacesAsync_WithNonExistingOrganization_ReturnsNotFound()
    {
        // Arrange
        _orgRepo.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var useCase = new OrganizationQueryUseCase(_orgRepo.Object);

        // Act
        var result = await useCase.GetWorkspacesAsync(Guid.NewGuid(), 1, 10);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Organização não encontrada.");
    }

    [Fact]
    public async Task GetCollaboratorsAsync_WithNonExistingOrganization_ReturnsNotFound()
    {
        // Arrange
        _orgRepo.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var useCase = new OrganizationQueryUseCase(_orgRepo.Object);

        // Act
        var result = await useCase.GetCollaboratorsAsync(Guid.NewGuid(), 1, 10);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Organização não encontrada.");
    }
}
