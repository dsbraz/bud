using Bud.Server.Application.DomainEvents.Notifications;
using Bud.Server.Domain.Events;
using Bud.Server.Domain.Repositories;
using Bud.Server.Application.Ports;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Notifications;

public sealed class DomainEventNotificationHandlersTests
{
    private static Mock<NotificationOrchestrator> CreateOrchestratorMock()
    {
        var repositoryMock = new Mock<INotificationRepository>();
        var recipientResolverMock = new Mock<INotificationRecipientResolver>();
        return new Mock<NotificationOrchestrator>(
            repositoryMock.Object,
            recipientResolverMock.Object);
    }

    [Fact]
    public async Task MissionCreatedHandler_ShouldNotifyMissionCreated()
    {
        var orchestratorMock = CreateOrchestratorMock();
        orchestratorMock
            .Setup(o => o.NotifyMissionCreatedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new MissionCreatedDomainEventConsumer(orchestratorMock.Object);
        var domainEvent = new MissionCreatedDomainEvent(Guid.NewGuid(), Guid.NewGuid());

        await handler.HandleAsync(domainEvent, CancellationToken.None);

        orchestratorMock.Verify(
            o => o.NotifyMissionCreatedAsync(domainEvent.MissionId, domainEvent.OrganizationId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MissionUpdatedHandler_ShouldNotifyMissionUpdated()
    {
        var orchestratorMock = CreateOrchestratorMock();
        orchestratorMock
            .Setup(o => o.NotifyMissionUpdatedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new MissionUpdatedDomainEventConsumer(orchestratorMock.Object);
        var domainEvent = new MissionUpdatedDomainEvent(Guid.NewGuid(), Guid.NewGuid());

        await handler.HandleAsync(domainEvent, CancellationToken.None);

        orchestratorMock.Verify(
            o => o.NotifyMissionUpdatedAsync(domainEvent.MissionId, domainEvent.OrganizationId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MissionDeletedHandler_ShouldNotifyMissionDeleted()
    {
        var orchestratorMock = CreateOrchestratorMock();
        orchestratorMock
            .Setup(o => o.NotifyMissionDeletedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new MissionDeletedDomainEventConsumer(orchestratorMock.Object);
        var domainEvent = new MissionDeletedDomainEvent(Guid.NewGuid(), Guid.NewGuid());

        await handler.HandleAsync(domainEvent, CancellationToken.None);

        orchestratorMock.Verify(
            o => o.NotifyMissionDeletedAsync(domainEvent.MissionId, domainEvent.OrganizationId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MetricCheckinCreatedHandler_ShouldNotifyMetricCheckinCreated()
    {
        var orchestratorMock = CreateOrchestratorMock();
        orchestratorMock
            .Setup(o => o.NotifyMetricCheckinCreatedAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new MetricCheckinCreatedDomainEventConsumer(orchestratorMock.Object);
        var domainEvent = new MetricCheckinCreatedDomainEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());

        await handler.HandleAsync(domainEvent, CancellationToken.None);

        orchestratorMock.Verify(
            o => o.NotifyMetricCheckinCreatedAsync(
                domainEvent.CheckinId,
                domainEvent.MetricId,
                domainEvent.OrganizationId,
                domainEvent.ExcludeCollaboratorId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
