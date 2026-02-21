using Bud.Server.Infrastructure.Repositories;
using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Workspaces;
using Bud.Server.Authorization;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Workspaces;

public sealed class WorkspaceCommandTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));
    private readonly Mock<IWorkspaceRepository> _wsRepo = new();
    private readonly Mock<IOrganizationRepository> _orgRepo = new();
    private readonly Mock<IApplicationAuthorizationGateway> _authGateway = new();

    private WorkspaceCommand CreateCommand()
        => new(_wsRepo.Object, _orgRepo.Object, _authGateway.Object);

    [Fact]
    public async Task CreateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        _authGateway
            .Setup(g => g.IsOrganizationOwnerAsync(User, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var workspaceCommand = CreateCommand();
        var request = new CreateWorkspaceRequest
        {
            Name = "Workspace",
            OrganizationId = Guid.NewGuid()
        };

        var result = await workspaceCommand.CreateAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        result.Error.Should().Be("Apenas o proprietário da organização pode criar workspaces.");
    }

    [Fact]
    public async Task UpdateAsync_WhenWorkspaceNotFound_ReturnsNotFound()
    {
        _wsRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Workspace?)null);

        var workspaceCommand = CreateCommand();

        var result = await workspaceCommand.UpdateAsync(User, Guid.NewGuid(), new UpdateWorkspaceRequest { Name = "Novo Nome" });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Workspace não encontrado.");
    }

    [Fact]
    public async Task DeleteAsync_WhenAuthorized_Succeeds()
    {
        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "Workspace", OrganizationId = Guid.NewGuid() };
        _wsRepo.Setup(r => r.GetByIdAsync(workspace.Id, It.IsAny<CancellationToken>())).ReturnsAsync(workspace);
        _wsRepo.Setup(r => r.HasMissionsAsync(workspace.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _authGateway
            .Setup(g => g.CanWriteOrganizationAsync(User, workspace.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var workspaceCommand = CreateCommand();

        var result = await workspaceCommand.DeleteAsync(User, workspace.Id);

        result.IsSuccess.Should().BeTrue();
        _wsRepo.Verify(r => r.RemoveAsync(workspace, It.IsAny<CancellationToken>()), Times.Once);
        _wsRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithMissions_ReturnsConflict()
    {
        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "Workspace", OrganizationId = Guid.NewGuid() };
        _wsRepo.Setup(r => r.GetByIdAsync(workspace.Id, It.IsAny<CancellationToken>())).ReturnsAsync(workspace);
        _wsRepo.Setup(r => r.HasMissionsAsync(workspace.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _authGateway
            .Setup(g => g.CanWriteOrganizationAsync(User, workspace.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var workspaceCommand = CreateCommand();

        var result = await workspaceCommand.DeleteAsync(User, workspace.Id);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
    }
}
