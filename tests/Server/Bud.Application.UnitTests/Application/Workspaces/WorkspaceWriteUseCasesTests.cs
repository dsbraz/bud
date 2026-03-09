using Bud.Domain.Repositories;
using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Mapping;
using Bud.Application.UseCases.Workspaces;
using Bud.Application.Ports;
using Bud.Shared.Contracts;
using Bud.Domain.Model;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Bud.Application.UnitTests.Application.Workspaces;

public sealed class WorkspaceWriteUseCasesTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));
    private readonly Mock<IWorkspaceRepository> _wsRepo = new();
    private readonly Mock<IOrganizationRepository> _orgRepo = new();
    private readonly Mock<IApplicationAuthorizationGateway> _authGateway = new();

    private CreateWorkspace CreateCreateWorkspace()
        => new(_wsRepo.Object, _orgRepo.Object, _authGateway.Object, NullLogger<CreateWorkspace>.Instance);

    private PatchWorkspace CreatePatchWorkspace()
        => new(_wsRepo.Object, _authGateway.Object, NullLogger<PatchWorkspace>.Instance);

    private DeleteWorkspace CreateDeleteWorkspace()
        => new(_wsRepo.Object, _authGateway.Object, NullLogger<DeleteWorkspace>.Instance);

    [Fact]
    public async Task CreateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        _authGateway
            .Setup(g => g.IsOrganizationOwnerAsync(User, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = CreateCreateWorkspace();
        var request = new CreateWorkspaceRequest
        {
            Name = "Workspace",
            OrganizationId = Guid.NewGuid()
        };

        var result = await useCase.ExecuteAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        result.Error.Should().Be("Apenas o proprietário da organização pode criar workspaces.");
    }

    [Fact]
    public async Task UpdateAsync_WhenWorkspaceNotFound_ReturnsNotFound()
    {
        _wsRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Workspace?)null);

        var useCase = CreatePatchWorkspace();

        var result = await useCase.ExecuteAsync(User, Guid.NewGuid(), new PatchWorkspaceRequest { Name = "Novo Nome" });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Workspace não encontrado.");
    }

    [Fact]
    public async Task DeleteAsync_WhenAuthorized_Succeeds()
    {
        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "Workspace", OrganizationId = Guid.NewGuid() };
        _wsRepo.Setup(r => r.GetByIdAsync(workspace.Id, It.IsAny<CancellationToken>())).ReturnsAsync(workspace);
        _wsRepo.Setup(r => r.HasGoalsAsync(workspace.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _authGateway
            .Setup(g => g.CanWriteOrganizationAsync(User, workspace.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = CreateDeleteWorkspace();

        var result = await useCase.ExecuteAsync(User, workspace.Id);

        result.IsSuccess.Should().BeTrue();
        _wsRepo.Verify(r => r.RemoveAsync(workspace, It.IsAny<CancellationToken>()), Times.Once);
        _wsRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithMissions_ReturnsConflict()
    {
        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "Workspace", OrganizationId = Guid.NewGuid() };
        _wsRepo.Setup(r => r.GetByIdAsync(workspace.Id, It.IsAny<CancellationToken>())).ReturnsAsync(workspace);
        _wsRepo.Setup(r => r.HasGoalsAsync(workspace.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _authGateway
            .Setup(g => g.CanWriteOrganizationAsync(User, workspace.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = CreateDeleteWorkspace();

        var result = await useCase.ExecuteAsync(User, workspace.Id);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
    }
}
