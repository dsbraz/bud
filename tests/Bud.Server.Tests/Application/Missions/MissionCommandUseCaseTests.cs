using System.Security.Claims;
using Bud.Server.Application.Common.Authorization;
using Bud.Server.Application.Common.ReadModel;
using Bud.Server.Application.Abstractions;
using Bud.Server.Application.Missions;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Missions;

public sealed class MissionCommandUseCaseTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));

    [Fact]
    public async Task CreateAsync_WhenScopeResolutionFails_ReturnsNotFound()
    {
        var missionService = new Mock<IMissionCommandService>(MockBehavior.Strict);
        var scopeResolver = new Mock<IMissionScopeResolver>();
        scopeResolver.Setup(s => s.ResolveScopeOrganizationIdAsync(
                MissionScopeType.Organization,
                It.IsAny<Guid>(),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<Guid>.NotFound("Organização não encontrada."));

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>(MockBehavior.Strict);
        var entityLookup = new Mock<IApplicationEntityLookup>(MockBehavior.Strict);
        var useCase = new MissionCommandUseCase(
            missionService.Object,
            scopeResolver.Object,
            authorizationGateway.Object,
            entityLookup.Object);

        var request = new CreateMissionRequest
        {
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Organization,
            ScopeId = Guid.NewGuid()
        };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Organização não encontrada.");
        missionService.VerifyNoOtherCalls();
        authorizationGateway.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var orgId = Guid.NewGuid();
        var missionService = new Mock<IMissionCommandService>(MockBehavior.Strict);
        var scopeResolver = new Mock<IMissionScopeResolver>();
        scopeResolver.Setup(s => s.ResolveScopeOrganizationIdAsync(
                MissionScopeType.Organization,
                It.IsAny<Guid>(),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<Guid>.Success(orgId));

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var entityLookup = new Mock<IApplicationEntityLookup>(MockBehavior.Strict);
        var useCase = new MissionCommandUseCase(
            missionService.Object,
            scopeResolver.Object,
            authorizationGateway.Object,
            entityLookup.Object);

        var request = new CreateMissionRequest
        {
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Organization,
            ScopeId = Guid.NewGuid()
        };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Forbidden);
        missionService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_WhenMissionNotFound_ReturnsNotFound()
    {
        var missionService = new Mock<IMissionCommandService>(MockBehavior.Strict);
        var scopeResolver = new Mock<IMissionScopeResolver>(MockBehavior.Strict);
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>(MockBehavior.Strict);
        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetMissionAsync(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mission?)null);

        var useCase = new MissionCommandUseCase(
            missionService.Object,
            scopeResolver.Object,
            authorizationGateway.Object,
            entityLookup.Object);

        var request = new UpdateMissionRequest
        {
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = MissionStatus.Planned
        };

        var result = await useCase.UpdateAsync(User, Guid.NewGuid(), request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        authorizationGateway.VerifyNoOtherCalls();
        missionService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = MissionStatus.Planned,
            OrganizationId = Guid.NewGuid()
        };

        var missionService = new Mock<IMissionCommandService>(MockBehavior.Strict);
        var scopeResolver = new Mock<IMissionScopeResolver>(MockBehavior.Strict);
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, mission.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetMissionAsync(mission.Id, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var useCase = new MissionCommandUseCase(
            missionService.Object,
            scopeResolver.Object,
            authorizationGateway.Object,
            entityLookup.Object);

        var result = await useCase.DeleteAsync(User, mission.Id);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Forbidden);
        missionService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_WhenAuthorizedAndCreated_DispatchesDomainEvent()
    {
        var orgId = Guid.NewGuid();

        var createdMission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Missão",
            OrganizationId = orgId,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = MissionStatus.Planned
        };

        var missionService = new Mock<IMissionCommandService>();
        missionService
            .Setup(s => s.CreateAsync(It.IsAny<CreateMissionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<Mission>.Success(createdMission));

        var scopeResolver = new Mock<IMissionScopeResolver>();
        scopeResolver
            .Setup(s => s.ResolveScopeOrganizationIdAsync(
                MissionScopeType.Organization,
                It.IsAny<Guid>(),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<Guid>.Success(orgId));

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var entityLookup = new Mock<IApplicationEntityLookup>(MockBehavior.Strict);
        var dispatcher = new Mock<Bud.Server.Application.Common.Events.IDomainEventDispatcher>();
        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<Bud.Server.Domain.Common.Events.IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new MissionCommandUseCase(
            missionService.Object,
            scopeResolver.Object,
            authorizationGateway.Object,
            entityLookup.Object,
            null,
            dispatcher.Object);

        var request = new CreateMissionRequest
        {
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Organization,
            ScopeId = Guid.NewGuid()
        };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeTrue();
        dispatcher.Verify(d => d.DispatchAsync(
            It.IsAny<Bud.Server.Domain.Missions.Events.MissionCreatedDomainEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenAuthorizedAndUpdated_DispatchesDomainEvent()
    {
        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Missão",
            OrganizationId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = MissionStatus.Planned
        };
        var request = new UpdateMissionRequest
        {
            Name = "Missão 2",
            StartDate = mission.StartDate,
            EndDate = mission.EndDate,
            Status = MissionStatus.Active
        };

        var missionService = new Mock<IMissionCommandService>();
        missionService
            .Setup(s => s.UpdateAsync(mission.Id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<Mission>.Success(new Mission
            {
                Id = mission.Id,
                Name = request.Name,
                OrganizationId = mission.OrganizationId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = request.Status
            }));

        var scopeResolver = new Mock<IMissionScopeResolver>(MockBehavior.Strict);
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, mission.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup.Setup(l => l.GetMissionAsync(mission.Id, false, It.IsAny<CancellationToken>())).ReturnsAsync(mission);

        var dispatcher = new Mock<Bud.Server.Application.Common.Events.IDomainEventDispatcher>();
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<Bud.Server.Domain.Common.Events.IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new MissionCommandUseCase(
            missionService.Object,
            scopeResolver.Object,
            authorizationGateway.Object,
            entityLookup.Object,
            null,
            dispatcher.Object);

        var result = await useCase.UpdateAsync(User, mission.Id, request);

        result.IsSuccess.Should().BeTrue();
        dispatcher.Verify(d => d.DispatchAsync(
            It.IsAny<Bud.Server.Domain.Missions.Events.MissionUpdatedDomainEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenAuthorizedAndDeleted_DispatchesDomainEvent()
    {
        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Missão",
            OrganizationId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = MissionStatus.Active
        };

        var missionService = new Mock<IMissionCommandService>();
        missionService
            .Setup(s => s.DeleteAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Success());

        var scopeResolver = new Mock<IMissionScopeResolver>(MockBehavior.Strict);
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, mission.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup.Setup(l => l.GetMissionAsync(mission.Id, false, It.IsAny<CancellationToken>())).ReturnsAsync(mission);

        var dispatcher = new Mock<Bud.Server.Application.Common.Events.IDomainEventDispatcher>();
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<Bud.Server.Domain.Common.Events.IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new MissionCommandUseCase(
            missionService.Object,
            scopeResolver.Object,
            authorizationGateway.Object,
            entityLookup.Object,
            null,
            dispatcher.Object);

        var result = await useCase.DeleteAsync(User, mission.Id);

        result.IsSuccess.Should().BeTrue();
        dispatcher.Verify(d => d.DispatchAsync(
            It.IsAny<Bud.Server.Domain.Missions.Events.MissionDeletedDomainEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
