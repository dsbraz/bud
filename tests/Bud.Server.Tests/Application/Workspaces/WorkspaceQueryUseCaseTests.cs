using Bud.Server.Application.Workspaces;
using Bud.Server.Services;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Workspaces;

public sealed class WorkspaceQueryUseCaseTests
{
    [Fact]
    public async Task GetByIdAsync_DelegatesToService()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var workspaceService = new Mock<IWorkspaceService>();
        workspaceService
            .Setup(s => s.GetByIdAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<Workspace>.Success(new Workspace { Id = workspaceId, Name = "Plataforma", OrganizationId = Guid.NewGuid() }));

        var useCase = new WorkspaceQueryUseCase(workspaceService.Object);

        // Act
        var result = await useCase.GetByIdAsync(workspaceId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(workspaceId);
    }

    [Fact]
    public async Task GetAllAsync_DelegatesToService()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var workspaceService = new Mock<IWorkspaceService>();
        workspaceService
            .Setup(s => s.GetAllAsync(organizationId, "ops", 2, 15, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<PagedResult<Workspace>>.Success(new PagedResult<Workspace>
            {
                Items = [],
                Page = 2,
                PageSize = 15,
                Total = 0
            }));

        var useCase = new WorkspaceQueryUseCase(workspaceService.Object);

        // Act
        var result = await useCase.GetAllAsync(organizationId, "ops", 2, 15);

        // Assert
        result.IsSuccess.Should().BeTrue();
        workspaceService.Verify(s => s.GetAllAsync(organizationId, "ops", 2, 15, It.IsAny<CancellationToken>()), Times.Once);
    }
}
