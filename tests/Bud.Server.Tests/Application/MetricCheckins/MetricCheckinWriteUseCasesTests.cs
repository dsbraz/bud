using Bud.Server.Domain.Repositories;
using Bud.Server.Infrastructure.Services;
using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.MetricCheckins;
using Bud.Server.Authorization;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using Bud.Server.Domain.Events;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.MetricCheckins;

public sealed class MetricCheckinWriteUseCasesTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));

    [Fact]
    public async Task CreateAsync_WhenCollaboratorNotIdentified_ReturnsForbidden()
    {
        var orgId = Guid.NewGuid();
        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = MissionStatus.Active,
            OrganizationId = orgId
        };
        var metric = new MissionMetric
        {
            Id = Guid.NewGuid(),
            Name = "Métrica",
            Type = MetricType.Qualitative,
            MissionId = mission.Id,
            Mission = mission,
            OrganizationId = orgId
        };

        var checkinRepository = new Mock<IMetricCheckinRepository>(MockBehavior.Strict);
        checkinRepository
            .Setup(r => r.GetMetricWithMissionAsync(metric.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);

        var collaboratorRepository = new Mock<ICollaboratorRepository>(MockBehavior.Strict);

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        authorizationGateway
            .Setup(g => g.CanAccessMissionScopeAsync(User, mission.WorkspaceId, mission.TeamId, mission.CollaboratorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(t => t.IsGlobalAdmin).Returns(false);
        tenantProvider.SetupGet(t => t.CollaboratorId).Returns((Guid?)null);

        var useCase = new RegisterMetricCheckin(
            checkinRepository.Object,
            collaboratorRepository.Object,
            authorizationGateway.Object,
            tenantProvider.Object);

        var request = new CreateMetricCheckinRequest
        {
            MissionMetricId = metric.Id,
            Text = "ok",
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        var result = await useCase.ExecuteAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        result.Error.Should().Be("Colaborador não identificado.");
        collaboratorRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_WhenSuccess_RegistersMetricCheckinCreatedDomainEvent()
    {
        var orgId = Guid.NewGuid();
        var collaboratorId = Guid.NewGuid();
        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = MissionStatus.Active,
            OrganizationId = orgId
        };
        var metric = new MissionMetric
        {
            Id = Guid.NewGuid(),
            Name = "Métrica",
            Type = MetricType.Quantitative,
            MissionId = mission.Id,
            Mission = mission,
            OrganizationId = orgId,
            QuantitativeType = QuantitativeMetricType.KeepAbove,
            MinValue = 0m,
            Unit = MetricUnit.Integer
        };

        var checkinRepository = new Mock<IMetricCheckinRepository>();
        checkinRepository
            .Setup(r => r.GetMetricWithMissionAsync(metric.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);
        checkinRepository
            .Setup(r => r.AddAsync(It.IsAny<MetricCheckin>(), It.IsAny<CancellationToken>()))
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
        authorizationGateway
            .Setup(g => g.CanAccessMissionScopeAsync(User, mission.WorkspaceId, mission.TeamId, mission.CollaboratorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(t => t.IsGlobalAdmin).Returns(false);
        tenantProvider.SetupGet(t => t.CollaboratorId).Returns(collaboratorId);

        var useCase = new RegisterMetricCheckin(
            checkinRepository.Object,
            collaboratorRepository.Object,
            authorizationGateway.Object,
            tenantProvider.Object);

        var request = new CreateMetricCheckinRequest
        {
            MissionMetricId = metric.Id,
            Value = 10m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        var result = await useCase.ExecuteAsync(User, request);

        result.IsSuccess.Should().BeTrue();
        checkinRepository.Verify(r => r.AddAsync(It.IsAny<MetricCheckin>(), It.IsAny<CancellationToken>()), Times.Once);
        checkinRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        var createdEvent = metric.DomainEvents.Should().ContainSingle().Subject;
        var created = createdEvent.Should().BeOfType<MetricCheckinCreatedDomainEvent>().Subject;
        created.CheckinId.Should().Be(result.Value!.Id);
        created.MissionMetricId.Should().Be(metric.Id);
        created.OrganizationId.Should().Be(orgId);
        created.ExcludeCollaboratorId.Should().Be(collaboratorId);
    }

    [Fact]
    public async Task CreateAsync_WhenMissionNotActive_ReturnsValidation()
    {
        var orgId = Guid.NewGuid();
        var collaboratorId = Guid.NewGuid();
        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = MissionStatus.Planned,
            OrganizationId = orgId
        };
        var metric = new MissionMetric
        {
            Id = Guid.NewGuid(),
            Name = "Métrica",
            Type = MetricType.Quantitative,
            MissionId = mission.Id,
            Mission = mission,
            OrganizationId = orgId
        };

        var checkinRepository = new Mock<IMetricCheckinRepository>(MockBehavior.Strict);
        checkinRepository
            .Setup(r => r.GetMetricWithMissionAsync(metric.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);

        var collaboratorRepository = new Mock<ICollaboratorRepository>();
        collaboratorRepository
            .Setup(r => r.GetByIdAsync(collaboratorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Collaborator { Id = collaboratorId, FullName = "Test", Email = "test@test.com", OrganizationId = orgId });

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        authorizationGateway
            .Setup(g => g.CanAccessMissionScopeAsync(User, mission.WorkspaceId, mission.TeamId, mission.CollaboratorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(t => t.IsGlobalAdmin).Returns(false);
        tenantProvider.SetupGet(t => t.CollaboratorId).Returns(collaboratorId);

        var useCase = new RegisterMetricCheckin(
            checkinRepository.Object,
            collaboratorRepository.Object,
            authorizationGateway.Object,
            tenantProvider.Object);

        var request = new CreateMetricCheckinRequest
        {
            MissionMetricId = metric.Id,
            Value = 10m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        var result = await useCase.ExecuteAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.Should().Be("Não é possível fazer check-in em métricas de missões que não estão ativas.");
    }

    [Fact]
    public async Task UpdateAsync_WhenNotAuthorAndNotGlobalAdmin_ReturnsForbidden()
    {
        var checkin = new MetricCheckin
        {
            Id = Guid.NewGuid(),
            MissionMetricId = Guid.NewGuid(),
            CollaboratorId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        var checkinRepository = new Mock<IMetricCheckinRepository>(MockBehavior.Strict);
        checkinRepository
            .Setup(r => r.GetByIdAsync(checkin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(checkin);

        var collaboratorRepository = new Mock<ICollaboratorRepository>(MockBehavior.Strict);

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, checkin.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(t => t.IsGlobalAdmin).Returns(false);
        tenantProvider.SetupGet(t => t.CollaboratorId).Returns(Guid.NewGuid());

        var useCase = new CorrectMetricCheckin(
            checkinRepository.Object,
            authorizationGateway.Object,
            tenantProvider.Object);

        var request = new UpdateMetricCheckinRequest
        {
            Value = 10m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 2
        };

        var result = await useCase.ExecuteAsync(User, checkin.Id, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        result.Error.Should().Be("Apenas o autor pode editar este check-in.");
        collaboratorRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_WhenGlobalAdmin_UpdatesViaRepository()
    {
        var orgId = Guid.NewGuid();
        var metricId = Guid.NewGuid();
        var checkin = new MetricCheckin
        {
            Id = Guid.NewGuid(),
            MissionMetricId = metricId,
            CollaboratorId = Guid.NewGuid(),
            OrganizationId = orgId,
            CheckinDate = DateTime.UtcNow,
            Value = 10m,
            ConfidenceLevel = 3
        };
        var metric = new MissionMetric
        {
            Id = metricId,
            Name = "Métrica",
            Type = MetricType.Quantitative,
            MissionId = Guid.NewGuid(),
            OrganizationId = orgId,
            QuantitativeType = QuantitativeMetricType.KeepAbove,
            MinValue = 0m,
            Unit = MetricUnit.Integer
        };

        var checkinRepository = new Mock<IMetricCheckinRepository>();
        checkinRepository
            .Setup(r => r.GetByIdAsync(checkin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(checkin);
        checkinRepository
            .Setup(r => r.GetMetricByIdAsync(metricId, It.IsAny<CancellationToken>()))
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

        var useCase = new CorrectMetricCheckin(
            checkinRepository.Object,
            authorizationGateway.Object,
            tenantProvider.Object);

        var request = new UpdateMetricCheckinRequest
        {
            Value = 25m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 4
        };

        var result = await useCase.ExecuteAsync(User, checkin.Id, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be(25m);
        checkinRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenGlobalAdmin_RemovesViaRepository()
    {
        var checkin = new MetricCheckin
        {
            Id = Guid.NewGuid(),
            MissionMetricId = Guid.NewGuid(),
            CollaboratorId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        var checkinRepository = new Mock<IMetricCheckinRepository>();
        checkinRepository
            .Setup(r => r.GetByIdAsync(checkin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(checkin);
        checkinRepository
            .Setup(r => r.RemoveAsync(checkin, It.IsAny<CancellationToken>()))
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

        var useCase = new DeleteMetricCheckin(
            checkinRepository.Object,
            authorizationGateway.Object,
            tenantProvider.Object);

        var result = await useCase.ExecuteAsync(User, checkin.Id);

        result.IsSuccess.Should().BeTrue();
        checkinRepository.Verify(r => r.RemoveAsync(checkin, It.IsAny<CancellationToken>()), Times.Once);
        checkinRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
