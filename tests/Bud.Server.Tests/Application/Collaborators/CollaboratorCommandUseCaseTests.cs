using Bud.Server.Application.Collaborators;
using Bud.Server.Application.Common.Authorization;
using Bud.Server.Application.Common.ReadModel;
using Bud.Server.MultiTenancy;
using Bud.Server.Services;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Bud.Server.Tests.Application.Collaborators;

public sealed class CollaboratorCommandUseCaseTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));

    [Fact]
    public async Task CreateAsync_WhenTenantSelectedAndUnauthorized_ReturnsForbidden()
    {
        var collaboratorService = new Mock<ICollaboratorService>(MockBehavior.Strict);
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        var tenantId = Guid.NewGuid();
        authorizationGateway
            .Setup(g => g.IsOrganizationOwnerAsync(User, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(t => t.TenantId).Returns(tenantId);
        var entityLookup = new Mock<IApplicationEntityLookup>(MockBehavior.Strict);

        var useCase = new CollaboratorCommandUseCase(
            collaboratorService.Object,
            authorizationGateway.Object,
            tenantProvider.Object,
            entityLookup.Object);

        var request = new CreateCollaboratorRequest
        {
            FullName = "User",
            Email = "user@test.com",
            Role = CollaboratorRole.IndividualContributor
        };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Forbidden);
        result.Error.Should().Be("Apenas o proprietário da organização pode criar colaboradores.");
        collaboratorService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_WhenCollaboratorNotFound_ReturnsNotFound()
    {
        var collaboratorService = new Mock<ICollaboratorService>(MockBehavior.Strict);
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>(MockBehavior.Strict);
        var tenantProvider = new Mock<ITenantProvider>(MockBehavior.Strict);
        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetCollaboratorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collaborator?)null);

        var useCase = new CollaboratorCommandUseCase(
            collaboratorService.Object,
            authorizationGateway.Object,
            tenantProvider.Object,
            entityLookup.Object);

        var request = new UpdateCollaboratorRequest
        {
            FullName = "User",
            Email = "user@test.com",
            Role = CollaboratorRole.IndividualContributor
        };

        var result = await useCase.UpdateAsync(User, Guid.NewGuid(), request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Colaborador não encontrado.");
        collaboratorService.VerifyNoOtherCalls();
        authorizationGateway.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateTeamsAsync_WhenAuthorized_DelegatesToService()
    {
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "User",
            Email = "user@test.com",
            OrganizationId = Guid.NewGuid()
        };

        var request = new UpdateCollaboratorTeamsRequest { TeamIds = [] };
        var collaboratorService = new Mock<ICollaboratorService>();
        collaboratorService
            .Setup(s => s.UpdateTeamsAsync(collaborator.Id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Success());

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.IsOrganizationOwnerAsync(User, collaborator.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var tenantProvider = new Mock<ITenantProvider>();
        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetCollaboratorAsync(collaborator.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collaborator);

        var useCase = new CollaboratorCommandUseCase(
            collaboratorService.Object,
            authorizationGateway.Object,
            tenantProvider.Object,
            entityLookup.Object);

        var result = await useCase.UpdateTeamsAsync(User, collaborator.Id, request);

        result.IsSuccess.Should().BeTrue();
        collaboratorService.Verify(s => s.UpdateTeamsAsync(collaborator.Id, request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenAuthorizedAndCreated_DispatchesDomainEvent()
    {
        var organizationId = Guid.NewGuid();
        var createdCollaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "User",
            Email = "user@test.com",
            OrganizationId = organizationId
        };

        var request = new CreateCollaboratorRequest
        {
            FullName = "User",
            Email = "user@test.com",
            Role = CollaboratorRole.IndividualContributor
        };

        var collaboratorService = new Mock<ICollaboratorService>();
        collaboratorService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<Collaborator>.Success(createdCollaborator));

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.IsOrganizationOwnerAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(t => t.TenantId).Returns(organizationId);

        var entityLookup = new Mock<IApplicationEntityLookup>(MockBehavior.Strict);
        var dispatcher = new Mock<Bud.Server.Application.Common.Events.IDomainEventDispatcher>();
        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<Bud.Server.Domain.Common.Events.IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new CollaboratorCommandUseCase(
            collaboratorService.Object,
            authorizationGateway.Object,
            tenantProvider.Object,
            entityLookup.Object,
            null,
            dispatcher.Object);

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeTrue();
        dispatcher.Verify(d => d.DispatchAsync(
            It.IsAny<Bud.Server.Domain.Collaborators.Events.CollaboratorCreatedDomainEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenAuthorizedAndUpdated_DispatchesDomainEvent()
    {
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "User",
            Email = "user@test.com",
            OrganizationId = Guid.NewGuid()
        };
        var request = new UpdateCollaboratorRequest
        {
            FullName = "User 2",
            Email = "user2@test.com",
            Role = CollaboratorRole.Leader
        };

        var collaboratorService = new Mock<ICollaboratorService>();
        collaboratorService
            .Setup(s => s.UpdateAsync(collaborator.Id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<Collaborator>.Success(new Collaborator
            {
                Id = collaborator.Id,
                FullName = request.FullName,
                Email = request.Email,
                OrganizationId = collaborator.OrganizationId,
                Role = request.Role
            }));

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.IsOrganizationOwnerAsync(User, collaborator.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var tenantProvider = new Mock<ITenantProvider>(MockBehavior.Strict);
        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup.Setup(l => l.GetCollaboratorAsync(collaborator.Id, It.IsAny<CancellationToken>())).ReturnsAsync(collaborator);

        var dispatcher = new Mock<Bud.Server.Application.Common.Events.IDomainEventDispatcher>();
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<Bud.Server.Domain.Common.Events.IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new CollaboratorCommandUseCase(
            collaboratorService.Object,
            authorizationGateway.Object,
            tenantProvider.Object,
            entityLookup.Object,
            null,
            dispatcher.Object);

        var result = await useCase.UpdateAsync(User, collaborator.Id, request);

        result.IsSuccess.Should().BeTrue();
        dispatcher.Verify(d => d.DispatchAsync(
            It.IsAny<Bud.Server.Domain.Collaborators.Events.CollaboratorUpdatedDomainEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenAuthorizedAndDeleted_DispatchesDomainEvent()
    {
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "User",
            Email = "user@test.com",
            OrganizationId = Guid.NewGuid()
        };

        var collaboratorService = new Mock<ICollaboratorService>();
        collaboratorService
            .Setup(s => s.DeleteAsync(collaborator.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Success());

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.IsOrganizationOwnerAsync(User, collaborator.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var tenantProvider = new Mock<ITenantProvider>(MockBehavior.Strict);
        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup.Setup(l => l.GetCollaboratorAsync(collaborator.Id, It.IsAny<CancellationToken>())).ReturnsAsync(collaborator);

        var dispatcher = new Mock<Bud.Server.Application.Common.Events.IDomainEventDispatcher>();
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<Bud.Server.Domain.Common.Events.IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new CollaboratorCommandUseCase(
            collaboratorService.Object,
            authorizationGateway.Object,
            tenantProvider.Object,
            entityLookup.Object,
            null,
            dispatcher.Object);

        var result = await useCase.DeleteAsync(User, collaborator.Id);

        result.IsSuccess.Should().BeTrue();
        dispatcher.Verify(d => d.DispatchAsync(
            It.IsAny<Bud.Server.Domain.Collaborators.Events.CollaboratorDeletedDomainEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
