using System.Security.Claims;
using Bud.Server.Application.Common.Authorization;
using Bud.Server.Application.Common.ReadModel;
using Bud.Server.Application.Workspaces;
using Bud.Server.Services;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Workspaces;

public sealed class WorkspaceCommandUseCaseTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));

    [Fact]
    public async Task CreateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var workspaceService = new Mock<IWorkspaceService>(MockBehavior.Strict);
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.IsOrganizationOwnerAsync(User, Guid.Parse("11111111-1111-1111-1111-111111111111"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var entityLookup = new Mock<IApplicationEntityLookup>(MockBehavior.Strict);
        var useCase = new WorkspaceCommandUseCase(workspaceService.Object, authorizationGateway.Object, entityLookup.Object);
        var request = new CreateWorkspaceRequest
        {
            Name = "Workspace",
            OrganizationId = Guid.Parse("11111111-1111-1111-1111-111111111111")
        };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Forbidden);
        result.Error.Should().Be("Apenas o proprietário da organização pode criar workspaces.");
        workspaceService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_WhenWorkspaceNotFound_ReturnsNotFound()
    {
        var workspaceService = new Mock<IWorkspaceService>(MockBehavior.Strict);
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>(MockBehavior.Strict);
        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetWorkspaceAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workspace?)null);

        var useCase = new WorkspaceCommandUseCase(workspaceService.Object, authorizationGateway.Object, entityLookup.Object);
        var request = new UpdateWorkspaceRequest { Name = "Novo Nome" };

        var result = await useCase.UpdateAsync(User, Guid.NewGuid(), request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Workspace não encontrado.");
        workspaceService.VerifyNoOtherCalls();
        authorizationGateway.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsync_WhenAuthorized_DelegatesToService()
    {
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Workspace",
            OrganizationId = Guid.NewGuid()
        };

        var workspaceService = new Mock<IWorkspaceService>();
        workspaceService
            .Setup(s => s.DeleteAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Success());

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanWriteOrganizationAsync(User, workspace.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetWorkspaceAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        var useCase = new WorkspaceCommandUseCase(workspaceService.Object, authorizationGateway.Object, entityLookup.Object);

        var result = await useCase.DeleteAsync(User, workspace.Id);

        result.IsSuccess.Should().BeTrue();
        workspaceService.Verify(s => s.DeleteAsync(workspace.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenAuthorizedAndCreated_DispatchesDomainEvent()
    {
        var organizationId = Guid.NewGuid();
        var createdWorkspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Workspace",
            OrganizationId = organizationId
        };

        var workspaceService = new Mock<IWorkspaceService>();
        workspaceService
            .Setup(s => s.CreateAsync(It.IsAny<CreateWorkspaceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<Workspace>.Success(createdWorkspace));

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.IsOrganizationOwnerAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var entityLookup = new Mock<IApplicationEntityLookup>(MockBehavior.Strict);
        var dispatcher = new Mock<Bud.Server.Application.Common.Events.IDomainEventDispatcher>();
        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<Bud.Server.Domain.Common.Events.IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new WorkspaceCommandUseCase(
            workspaceService.Object,
            authorizationGateway.Object,
            entityLookup.Object,
            null,
            dispatcher.Object);

        var request = new CreateWorkspaceRequest
        {
            Name = "Workspace",
            OrganizationId = organizationId
        };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeTrue();
        dispatcher.Verify(d => d.DispatchAsync(
            It.IsAny<Bud.Server.Domain.Workspaces.Events.WorkspaceCreatedDomainEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenAuthorizedAndUpdated_DispatchesDomainEvent()
    {
        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "W", OrganizationId = Guid.NewGuid() };
        var request = new UpdateWorkspaceRequest { Name = "W2" };

        var workspaceService = new Mock<IWorkspaceService>();
        workspaceService
            .Setup(s => s.UpdateAsync(workspace.Id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<Workspace>.Success(new Workspace
            {
                Id = workspace.Id,
                Name = request.Name,
                OrganizationId = workspace.OrganizationId
            }));

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanWriteOrganizationAsync(User, workspace.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup.Setup(l => l.GetWorkspaceAsync(workspace.Id, It.IsAny<CancellationToken>())).ReturnsAsync(workspace);

        var dispatcher = new Mock<Bud.Server.Application.Common.Events.IDomainEventDispatcher>();
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<Bud.Server.Domain.Common.Events.IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new WorkspaceCommandUseCase(workspaceService.Object, authorizationGateway.Object, entityLookup.Object, null, dispatcher.Object);

        var result = await useCase.UpdateAsync(User, workspace.Id, request);

        result.IsSuccess.Should().BeTrue();
        dispatcher.Verify(d => d.DispatchAsync(
            It.IsAny<Bud.Server.Domain.Workspaces.Events.WorkspaceUpdatedDomainEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenAuthorizedAndDeleted_DispatchesDomainEvent()
    {
        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "W", OrganizationId = Guid.NewGuid() };

        var workspaceService = new Mock<IWorkspaceService>();
        workspaceService
            .Setup(s => s.DeleteAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Success());

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanWriteOrganizationAsync(User, workspace.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup.Setup(l => l.GetWorkspaceAsync(workspace.Id, It.IsAny<CancellationToken>())).ReturnsAsync(workspace);

        var dispatcher = new Mock<Bud.Server.Application.Common.Events.IDomainEventDispatcher>();
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<Bud.Server.Domain.Common.Events.IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new WorkspaceCommandUseCase(workspaceService.Object, authorizationGateway.Object, entityLookup.Object, null, dispatcher.Object);

        var result = await useCase.DeleteAsync(User, workspace.Id);

        result.IsSuccess.Should().BeTrue();
        dispatcher.Verify(d => d.DispatchAsync(
            It.IsAny<Bud.Server.Domain.Workspaces.Events.WorkspaceDeletedDomainEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
