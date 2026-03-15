using Bud.Application.EventHandlers;
using Moq;
using Xunit;

namespace Bud.Application.UnitTests.Application.EventHandlers;

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
    public async Task GoalCreatedHandler_ShouldNotifyGoalCreated()
    {
        var orchestratorMock = CreateOrchestratorMock();
        orchestratorMock
            .Setup(o => o.NotifyGoalCreatedAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new GoalCreatedDomainEventNotifier(orchestratorMock.Object);
        var domainEvent = new GoalCreatedDomainEvent(Guid.NewGuid(), Guid.NewGuid(), "Meta", Guid.NewGuid());

        await handler.HandleAsync(domainEvent, CancellationToken.None);

        orchestratorMock.Verify(
            o => o.NotifyGoalCreatedAsync(
                domainEvent.GoalId,
                domainEvent.OrganizationId,
                domainEvent.GoalName,
                domainEvent.ActorCollaboratorId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GoalUpdatedHandler_ShouldNotifyGoalUpdated()
    {
        var orchestratorMock = CreateOrchestratorMock();
        orchestratorMock
            .Setup(o => o.NotifyGoalUpdatedAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new GoalUpdatedDomainEventNotifier(orchestratorMock.Object);
        var domainEvent = new GoalUpdatedDomainEvent(Guid.NewGuid(), Guid.NewGuid(), "Meta", Guid.NewGuid());

        await handler.HandleAsync(domainEvent, CancellationToken.None);

        orchestratorMock.Verify(
            o => o.NotifyGoalUpdatedAsync(
                domainEvent.GoalId,
                domainEvent.OrganizationId,
                domainEvent.GoalName,
                domainEvent.ActorCollaboratorId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GoalDeletedHandler_ShouldNotifyGoalDeleted()
    {
        var orchestratorMock = CreateOrchestratorMock();
        orchestratorMock
            .Setup(o => o.NotifyGoalDeletedAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new GoalDeletedDomainEventNotifier(orchestratorMock.Object);
        var domainEvent = new GoalDeletedDomainEvent(Guid.NewGuid(), Guid.NewGuid(), "Meta", Guid.NewGuid());

        await handler.HandleAsync(domainEvent, CancellationToken.None);

        orchestratorMock.Verify(
            o => o.NotifyGoalDeletedAsync(
                domainEvent.GoalId,
                domainEvent.OrganizationId,
                domainEvent.GoalName,
                domainEvent.ActorCollaboratorId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckinCreatedHandler_ShouldNotifyCheckinCreated()
    {
        var orchestratorMock = CreateOrchestratorMock();
        orchestratorMock
            .Setup(o => o.NotifyCheckinCreatedAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CheckinCreatedDomainEventNotifier(orchestratorMock.Object);
        var domainEvent = new CheckinCreatedDomainEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Vendas mensais");

        await handler.HandleAsync(domainEvent, CancellationToken.None);

        orchestratorMock.Verify(
            o => o.NotifyCheckinCreatedAsync(
                domainEvent.CheckinId,
                domainEvent.IndicatorId,
                domainEvent.OrganizationId,
                domainEvent.CollaboratorId,
                domainEvent.IndicatorName,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
