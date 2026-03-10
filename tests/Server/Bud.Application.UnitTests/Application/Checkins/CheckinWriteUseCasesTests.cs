using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Ports;
using Bud.Shared.Contracts;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Bud.Application.UnitTests.Application.Checkins;

public sealed class CheckinWriteUseCasesTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));

    [Fact]
    public async Task CreateAsync_WhenCollaboratorNotIdentified_ReturnsForbidden()
    {
        var orgId = Guid.NewGuid();
        var mission = new Goal
        {
            Id = Guid.NewGuid(),
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = GoalStatus.Active,
            OrganizationId = orgId
        };
        var metric = new Indicator
        {
            Id = Guid.NewGuid(),
            Name = "Métrica",
            Type = IndicatorType.Qualitative,
            GoalId = mission.Id,
            Goal = mission,
            OrganizationId = orgId
        };

        var checkinRepository = new Mock<IIndicatorRepository>(MockBehavior.Strict);
        checkinRepository
            .Setup(r => r.GetIndicatorWithGoalAsync(metric.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);

        var collaboratorRepository = new Mock<ICollaboratorRepository>(MockBehavior.Strict);

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(t => t.IsGlobalAdmin).Returns(false);
        tenantProvider.SetupGet(t => t.CollaboratorId).Returns((Guid?)null);

        var useCase = new CreateCheckin(
            checkinRepository.Object,
            collaboratorRepository.Object,
            authorizationGateway.Object,
            tenantProvider.Object,
            NullLogger<CreateCheckin>.Instance);

        var request = new CreateCheckinRequest
        {
            Text = "ok",
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        var result = await useCase.ExecuteAsync(User, metric.Id, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        result.Error.Should().Be("Colaborador não identificado.");
        collaboratorRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_WhenCollaboratorDoesNotExist_ReturnsNotFound()
    {
        var orgId = Guid.NewGuid();
        var collaboratorId = Guid.NewGuid();
        var goal = new Goal
        {
            Id = Guid.NewGuid(),
            Name = "Meta",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = GoalStatus.Active,
            OrganizationId = orgId
        };
        var indicator = new Indicator
        {
            Id = Guid.NewGuid(),
            Name = "Indicador",
            Type = IndicatorType.Qualitative,
            GoalId = goal.Id,
            Goal = goal,
            OrganizationId = orgId
        };

        var indicatorRepository = new Mock<IIndicatorRepository>(MockBehavior.Strict);
        indicatorRepository
            .Setup(r => r.GetIndicatorWithGoalAsync(indicator.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(indicator);

        var collaboratorRepository = new Mock<ICollaboratorRepository>(MockBehavior.Strict);
        collaboratorRepository
            .Setup(r => r.GetByIdAsync(collaboratorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collaborator?)null);

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>(MockBehavior.Strict);
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var tenantProvider = new Mock<ITenantProvider>(MockBehavior.Strict);
        tenantProvider.SetupGet(t => t.CollaboratorId).Returns(collaboratorId);

        var useCase = new CreateCheckin(
            indicatorRepository.Object,
            collaboratorRepository.Object,
            authorizationGateway.Object,
            tenantProvider.Object,
            NullLogger<CreateCheckin>.Instance);

        var request = new CreateCheckinRequest
        {
            Text = "ok",
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        var result = await useCase.ExecuteAsync(User, indicator.Id, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Colaborador não encontrado.");
    }

    [Fact]
    public async Task CreateAsync_WhenSuccess_RegistersCheckinCreatedDomainEvent()
    {
        var orgId = Guid.NewGuid();
        var collaboratorId = Guid.NewGuid();
        var mission = new Goal
        {
            Id = Guid.NewGuid(),
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = GoalStatus.Active,
            OrganizationId = orgId
        };
        var metric = new Indicator
        {
            Id = Guid.NewGuid(),
            Name = "Métrica",
            Type = IndicatorType.Quantitative,
            GoalId = mission.Id,
            Goal = mission,
            OrganizationId = orgId,
            QuantitativeType = QuantitativeIndicatorType.KeepAbove,
            MinValue = 0m,
            Unit = IndicatorUnit.Integer
        };

        var checkinRepository = new Mock<IIndicatorRepository>();
        checkinRepository
            .Setup(r => r.GetIndicatorWithGoalAsync(metric.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);
        checkinRepository
            .Setup(r => r.AddCheckinAsync(It.IsAny<Checkin>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        checkinRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var collaboratorRepository = new Mock<ICollaboratorRepository>();
        collaboratorRepository
            .Setup(r => r.GetByIdAsync(collaboratorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Collaborator { Id = collaboratorId, FullName = "Test", Email = "test@test.com", OrganizationId = orgId });

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(t => t.IsGlobalAdmin).Returns(false);
        tenantProvider.SetupGet(t => t.CollaboratorId).Returns(collaboratorId);

        var useCase = new CreateCheckin(
            checkinRepository.Object,
            collaboratorRepository.Object,
            authorizationGateway.Object,
            tenantProvider.Object,
            NullLogger<CreateCheckin>.Instance);

        var request = new CreateCheckinRequest
        {
            Value = 10m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        var result = await useCase.ExecuteAsync(User, metric.Id, request);

        result.IsSuccess.Should().BeTrue();
        checkinRepository.Verify(r => r.AddCheckinAsync(It.IsAny<Checkin>(), It.IsAny<CancellationToken>()), Times.Once);
        checkinRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        var createdEvent = metric.DomainEvents.Should().ContainSingle().Subject;
        var created = createdEvent.Should().BeOfType<CheckinCreatedDomainEvent>().Subject;
        created.CheckinId.Should().Be(result.Value!.Id);
        created.IndicatorId.Should().Be(metric.Id);
        created.OrganizationId.Should().Be(orgId);
        created.CollaboratorId.Should().Be(collaboratorId);
    }

    [Fact]
    public async Task CreateAsync_WhenMissionNotActive_ReturnsValidation()
    {
        var orgId = Guid.NewGuid();
        var collaboratorId = Guid.NewGuid();
        var mission = new Goal
        {
            Id = Guid.NewGuid(),
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = GoalStatus.Planned,
            OrganizationId = orgId
        };
        var metric = new Indicator
        {
            Id = Guid.NewGuid(),
            Name = "Métrica",
            Type = IndicatorType.Quantitative,
            GoalId = mission.Id,
            Goal = mission,
            OrganizationId = orgId
        };

        var checkinRepository = new Mock<IIndicatorRepository>(MockBehavior.Strict);
        checkinRepository
            .Setup(r => r.GetIndicatorWithGoalAsync(metric.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);

        var collaboratorRepository = new Mock<ICollaboratorRepository>();
        collaboratorRepository
            .Setup(r => r.GetByIdAsync(collaboratorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Collaborator { Id = collaboratorId, FullName = "Test", Email = "test@test.com", OrganizationId = orgId });

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(t => t.IsGlobalAdmin).Returns(false);
        tenantProvider.SetupGet(t => t.CollaboratorId).Returns(collaboratorId);

        var useCase = new CreateCheckin(
            checkinRepository.Object,
            collaboratorRepository.Object,
            authorizationGateway.Object,
            tenantProvider.Object,
            NullLogger<CreateCheckin>.Instance);

        var request = new CreateCheckinRequest
        {
            Value = 10m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        var result = await useCase.ExecuteAsync(User, metric.Id, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.Should().Be("Não é possível fazer check-in em indicadores de metas que não estão ativas.");
    }

    [Fact]
    public async Task UpdateAsync_WhenNotAuthorAndNotGlobalAdmin_ReturnsForbidden()
    {
        var checkin = new Checkin
        {
            Id = Guid.NewGuid(),
            CollaboratorId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        var checkinRepository = new Mock<IIndicatorRepository>(MockBehavior.Strict);
        checkinRepository
            .Setup(r => r.GetCheckinByIdAsync(checkin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(checkin);

        var collaboratorRepository = new Mock<ICollaboratorRepository>(MockBehavior.Strict);

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, checkin.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(t => t.IsGlobalAdmin).Returns(false);
        tenantProvider.SetupGet(t => t.CollaboratorId).Returns(Guid.NewGuid());

        var useCase = new PatchCheckin(
            checkinRepository.Object,
            authorizationGateway.Object,
            tenantProvider.Object,
            NullLogger<PatchCheckin>.Instance);

        var request = new PatchCheckinRequest
        {
            Value = 10m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 2
        };

        var result = await useCase.ExecuteAsync(User, checkin.IndicatorId, checkin.Id, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        result.Error.Should().Be("Apenas o autor pode editar este check-in.");
        collaboratorRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_WhenGlobalAdmin_UpdatesViaRepository()
    {
        var orgId = Guid.NewGuid();
        var indicatorId = Guid.NewGuid();
        var checkin = new Checkin
        {
            Id = Guid.NewGuid(),
            CollaboratorId = Guid.NewGuid(),
            OrganizationId = orgId,
            IndicatorId = indicatorId,
            CheckinDate = DateTime.UtcNow,
            Value = 10m,
            ConfidenceLevel = 3
        };
        var metric = new Indicator
        {
            Id = indicatorId,
            Name = "Métrica",
            Type = IndicatorType.Quantitative,
            GoalId = Guid.NewGuid(),
            OrganizationId = orgId,
            QuantitativeType = QuantitativeIndicatorType.KeepAbove,
            MinValue = 0m,
            Unit = IndicatorUnit.Integer
        };

        var checkinRepository = new Mock<IIndicatorRepository>();
        checkinRepository
            .Setup(r => r.GetCheckinByIdAsync(checkin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(checkin);
        checkinRepository
            .Setup(r => r.GetByIdAsync(indicatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);
        checkinRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var collaboratorRepository = new Mock<ICollaboratorRepository>();

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(t => t.IsGlobalAdmin).Returns(true);

        var useCase = new PatchCheckin(
            checkinRepository.Object,
            authorizationGateway.Object,
            tenantProvider.Object,
            NullLogger<PatchCheckin>.Instance);

        var request = new PatchCheckinRequest
        {
            Value = 25m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 4
        };

        var result = await useCase.ExecuteAsync(User, checkin.IndicatorId, checkin.Id, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be(25m);
        checkinRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenGlobalAdmin_RemovesViaRepository()
    {
        var checkin = new Checkin
        {
            Id = Guid.NewGuid(),
            CollaboratorId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        var checkinRepository = new Mock<IIndicatorRepository>();
        checkinRepository
            .Setup(r => r.GetCheckinByIdAsync(checkin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(checkin);
        checkinRepository
            .Setup(r => r.RemoveCheckinAsync(checkin, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        checkinRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var collaboratorRepository = new Mock<ICollaboratorRepository>();

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, checkin.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(t => t.IsGlobalAdmin).Returns(true);

        var useCase = new DeleteCheckin(
            checkinRepository.Object,
            authorizationGateway.Object,
            tenantProvider.Object,
            NullLogger<DeleteCheckin>.Instance);

        var result = await useCase.ExecuteAsync(User, checkin.IndicatorId, checkin.Id);

        result.IsSuccess.Should().BeTrue();
        checkinRepository.Verify(r => r.RemoveCheckinAsync(checkin, It.IsAny<CancellationToken>()), Times.Once);
        checkinRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
