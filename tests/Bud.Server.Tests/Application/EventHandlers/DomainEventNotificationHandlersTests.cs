using Bud.Server.Application.EventHandlers;
using Bud.Server.Domain.Events;
using Bud.Server.Domain.Repositories;
using Bud.Server.Application.Ports;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.EventHandlers;

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
            .Setup(o => o.NotifyGoalCreatedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new GoalCreatedDomainEventNotifier(orchestratorMock.Object);
        var domainEvent = new GoalCreatedDomainEvent(Guid.NewGuid(), Guid.NewGuid());

        await handler.HandleAsync(domainEvent, CancellationToken.None);

        orchestratorMock.Verify(
            o => o.NotifyGoalCreatedAsync(domainEvent.GoalId, domainEvent.OrganizationId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MissionUpdatedHandler_ShouldNotifyMissionUpdated()
    {
        var orchestratorMock = CreateOrchestratorMock();
        orchestratorMock
            .Setup(o => o.NotifyGoalUpdatedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new GoalUpdatedDomainEventNotifier(orchestratorMock.Object);
        var domainEvent = new GoalUpdatedDomainEvent(Guid.NewGuid(), Guid.NewGuid());

        await handler.HandleAsync(domainEvent, CancellationToken.None);

        orchestratorMock.Verify(
            o => o.NotifyGoalUpdatedAsync(domainEvent.GoalId, domainEvent.OrganizationId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MissionDeletedHandler_ShouldNotifyMissionDeleted()
    {
        var orchestratorMock = CreateOrchestratorMock();
        orchestratorMock
            .Setup(o => o.NotifyGoalDeletedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new GoalDeletedDomainEventNotifier(orchestratorMock.Object);
        var domainEvent = new GoalDeletedDomainEvent(Guid.NewGuid(), Guid.NewGuid());

        await handler.HandleAsync(domainEvent, CancellationToken.None);

        orchestratorMock.Verify(
            o => o.NotifyGoalDeletedAsync(domainEvent.GoalId, domainEvent.OrganizationId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MetricCheckinCreatedHandler_ShouldNotifyMetricCheckinCreated()
    {
        var orchestratorMock = CreateOrchestratorMock();
        orchestratorMock
            .Setup(o => o.NotifyCheckinCreatedAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CheckinCreatedDomainEventNotifier(orchestratorMock.Object);
        var domainEvent = new CheckinCreatedDomainEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());

        await handler.HandleAsync(domainEvent, CancellationToken.None);

        orchestratorMock.Verify(
            o => o.NotifyCheckinCreatedAsync(
                domainEvent.CheckinId,
                domainEvent.IndicatorId,
                domainEvent.OrganizationId,
                domainEvent.CollaboratorId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
