using Bud.Server.Infrastructure.Repositories;
using Bud.Server.Application.Common;
using Bud.Server.Application.Workspaces;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Workspaces;

public sealed class WorkspaceQueryUseCaseTests
{
    private readonly Mock<IWorkspaceRepository> _wsRepo = new();

    [Fact]
    public async Task GetByIdAsync_WithExistingWorkspace_ReturnsSuccess()
    {
        var workspaceId = Guid.NewGuid();
        _wsRepo.Setup(r => r.GetByIdAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Workspace { Id = workspaceId, Name = "Plataforma", OrganizationId = Guid.NewGuid() });

        var useCase = new WorkspaceQueryUseCase(_wsRepo.Object);

        var result = await useCase.GetByIdAsync(workspaceId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(workspaceId);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ReturnsNotFound()
    {
        _wsRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Workspace?)null);

        var useCase = new WorkspaceQueryUseCase(_wsRepo.Object);

        var result = await useCase.GetByIdAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Workspace não encontrado.");
    }

    [Fact]
    public async Task GetAllAsync_DelegatesToRepository()
    {
        var organizationId = Guid.NewGuid();
        _wsRepo.Setup(r => r.GetAllAsync(organizationId, "ops", 2, 15, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Workspace> { Items = [], Page = 2, PageSize = 15, Total = 0 });

        var useCase = new WorkspaceQueryUseCase(_wsRepo.Object);

        var result = await useCase.GetAllAsync(organizationId, "ops", 2, 15);

        result.IsSuccess.Should().BeTrue();
        _wsRepo.Verify(r => r.GetAllAsync(organizationId, "ops", 2, 15, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTeamsAsync_WithNonExistingWorkspace_ReturnsNotFound()
    {
        _wsRepo.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var useCase = new WorkspaceQueryUseCase(_wsRepo.Object);

        var result = await useCase.GetTeamsAsync(Guid.NewGuid(), 1, 10);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Workspace não encontrado.");
    }
}
