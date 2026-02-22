using Bud.Server.Domain.Repositories;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Application.UseCases.Workspaces;
using Bud.Shared.Contracts;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Workspaces;

public sealed class WorkspaceReadUseCasesTests
{
    private readonly Mock<IWorkspaceRepository> _wsRepo = new();

    [Fact]
    public async Task GetByIdAsync_WithExistingWorkspace_ReturnsSuccess()
    {
        var workspaceId = Guid.NewGuid();
        _wsRepo.Setup(r => r.GetByIdAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Workspace { Id = workspaceId, Name = "Plataforma", OrganizationId = Guid.NewGuid() });

        var useCase = new GetWorkspaceById(_wsRepo.Object);

        var result = await useCase.ExecuteAsync(workspaceId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(workspaceId);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ReturnsNotFound()
    {
        _wsRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Workspace?)null);

        var useCase = new GetWorkspaceById(_wsRepo.Object);

        var result = await useCase.ExecuteAsync(Guid.NewGuid());

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

        var useCase = new ListWorkspaces(_wsRepo.Object);

        var result = await useCase.ExecuteAsync(organizationId, "ops", 2, 15);

        result.IsSuccess.Should().BeTrue();
        _wsRepo.Verify(r => r.GetAllAsync(organizationId, "ops", 2, 15, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTeamsAsync_WithNonExistingWorkspace_ReturnsNotFound()
    {
        _wsRepo.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var useCase = new ListWorkspaceTeams(_wsRepo.Object);

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), 1, 10);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Workspace não encontrado.");
    }
}
