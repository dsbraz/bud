using Bud.Server.Domain.Repositories;
using Bud.Server.Application.Ports;
using Bud.Server.Application.EventHandlers;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.EventHandlers;

public class NotificationOrchestratorTests
{
    private readonly Mock<INotificationRepository> _repoMock = new();
    private readonly Mock<INotificationRecipientResolver> _recipientResolverMock = new();
    private readonly NotificationOrchestrator _orchestrator;

    public NotificationOrchestratorTests()
    {
        _orchestrator = new NotificationOrchestrator(
            _repoMock.Object,
            _recipientResolverMock.Object);
    }

    [Fact]
    public async Task NotifyGoalCreatedAsync_WithActorAndRecipients_CreatesNotificationsWithActorName()
    {
        // Arrange
        var goalId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var recipients = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        _recipientResolverMock
            .Setup(r => r.ResolveGoalRecipientsAsync(goalId, organizationId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipients);

        _recipientResolverMock
            .Setup(r => r.ResolveCollaboratorNameAsync(actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Maria Silva");

        // Act
        await _orchestrator.NotifyGoalCreatedAsync(goalId, organizationId, "Aumentar vendas Q1", actorId);

        // Assert
        _repoMock.Verify(
            r => r.AddRangeAsync(
                It.Is<IEnumerable<Notification>>(n =>
                    n.Count() == 2 &&
                    n.All(x => x.Type == NotificationType.GoalCreated &&
                               x.Title == "Nova meta criada" &&
                               x.Message == "Maria Silva criou a meta 'Aumentar vendas Q1'")),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyGoalCreatedAsync_WithoutActor_CreatesNotificationsWithFallbackMessage()
    {
        // Arrange
        var goalId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var recipients = new List<Guid> { Guid.NewGuid() };

        _recipientResolverMock
            .Setup(r => r.ResolveGoalRecipientsAsync(goalId, organizationId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipients);

        // Act
        await _orchestrator.NotifyGoalCreatedAsync(goalId, organizationId, "Aumentar vendas Q1", null);

        // Assert
        _repoMock.Verify(
            r => r.AddRangeAsync(
                It.Is<IEnumerable<Notification>>(n =>
                    n.All(x => x.Message == "Uma nova meta foi criada: 'Aumentar vendas Q1'")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyGoalCreatedAsync_WithEmptyRecipients_DoesNotCreateNotifications()
    {
        // Arrange
        var goalId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        _recipientResolverMock
            .Setup(r => r.ResolveGoalRecipientsAsync(goalId, organizationId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        // Act
        await _orchestrator.NotifyGoalCreatedAsync(goalId, organizationId, "Meta", null);

        // Assert
        _repoMock.Verify(
            r => r.AddRangeAsync(It.IsAny<IEnumerable<Notification>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NotifyGoalUpdatedAsync_WithActor_CreatesNotificationsWithActorName()
    {
        // Arrange
        var goalId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var recipients = new List<Guid> { Guid.NewGuid() };

        _recipientResolverMock
            .Setup(r => r.ResolveGoalRecipientsAsync(goalId, organizationId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipients);

        _recipientResolverMock
            .Setup(r => r.ResolveCollaboratorNameAsync(actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("João Souza");

        // Act
        await _orchestrator.NotifyGoalUpdatedAsync(goalId, organizationId, "Aumentar vendas Q1", actorId);

        // Assert
        _repoMock.Verify(
            r => r.AddRangeAsync(
                It.Is<IEnumerable<Notification>>(n =>
                    n.Count() == 1 &&
                    n.All(x => x.Type == NotificationType.GoalUpdated &&
                               x.Title == "Meta atualizada" &&
                               x.Message == "João Souza atualizou a meta 'Aumentar vendas Q1'")),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyGoalDeletedAsync_WithActor_CreatesNotificationsWithActorName()
    {
        // Arrange
        var goalId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var recipients = new List<Guid> { Guid.NewGuid() };

        _recipientResolverMock
            .Setup(r => r.ResolveGoalRecipientsAsync(goalId, organizationId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipients);

        _recipientResolverMock
            .Setup(r => r.ResolveCollaboratorNameAsync(actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Maria Silva");

        // Act
        await _orchestrator.NotifyGoalDeletedAsync(goalId, organizationId, "Aumentar vendas Q1", actorId);

        // Assert
        _repoMock.Verify(
            r => r.AddRangeAsync(
                It.Is<IEnumerable<Notification>>(n =>
                    n.Count() == 1 &&
                    n.All(x => x.Type == NotificationType.GoalDeleted &&
                               x.Title == "Meta removida" &&
                               x.Message == "Maria Silva removeu a meta 'Aumentar vendas Q1'")),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyCheckinCreatedAsync_WithActorAndRecipients_CreatesNotificationsWithIndicatorName()
    {
        // Arrange
        var checkinId = Guid.NewGuid();
        var indicatorId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        var recipients = new List<Guid> { Guid.NewGuid() };

        _recipientResolverMock
            .Setup(r => r.ResolveGoalIdFromIndicatorAsync(indicatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goalId);

        _recipientResolverMock
            .Setup(r => r.ResolveGoalRecipientsAsync(goalId, organizationId, actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipients);

        _recipientResolverMock
            .Setup(r => r.ResolveCollaboratorNameAsync(actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("João Souza");

        // Act
        await _orchestrator.NotifyCheckinCreatedAsync(checkinId, indicatorId, organizationId, actorId, "Vendas mensais");

        // Assert
        _repoMock.Verify(
            r => r.AddRangeAsync(
                It.Is<IEnumerable<Notification>>(n =>
                    n.Count() == 1 &&
                    n.All(x => x.Type == NotificationType.CheckinCreated &&
                               x.Title == "Novo check-in registrado" &&
                               x.Message == "João Souza registrou um check-in no indicador 'Vendas mensais'" &&
                               x.RelatedEntityId == checkinId &&
                               x.RelatedEntityType == "Checkin")),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyCheckinCreatedAsync_WhenGoalNotFound_DoesNotCreateNotifications()
    {
        // Arrange
        var checkinId = Guid.NewGuid();
        var indicatorId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        _recipientResolverMock
            .Setup(r => r.ResolveGoalIdFromIndicatorAsync(indicatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        // Act
        await _orchestrator.NotifyCheckinCreatedAsync(checkinId, indicatorId, organizationId, null, "Indicador");

        // Assert
        _recipientResolverMock.Verify(
            r => r.ResolveGoalRecipientsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _repoMock.Verify(
            r => r.AddRangeAsync(It.IsAny<IEnumerable<Notification>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task NotifyCheckinCreatedAsync_WithEmptyRecipients_DoesNotCreateNotifications()
    {
        // Arrange
        var checkinId = Guid.NewGuid();
        var indicatorId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var goalId = Guid.NewGuid();

        _recipientResolverMock
            .Setup(r => r.ResolveGoalIdFromIndicatorAsync(indicatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goalId);

        _recipientResolverMock
            .Setup(r => r.ResolveGoalRecipientsAsync(goalId, organizationId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        // Act
        await _orchestrator.NotifyCheckinCreatedAsync(checkinId, indicatorId, organizationId, null, "Indicador");

        // Assert
        _repoMock.Verify(
            r => r.AddRangeAsync(It.IsAny<IEnumerable<Notification>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
