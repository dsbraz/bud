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
    public async Task NotifyMissionCreatedAsync_WithRecipients_CreatesNotifications()
    {
        // Arrange
        var goalId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var recipients = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        _recipientResolverMock
            .Setup(r => r.ResolveGoalRecipientsAsync(goalId, organizationId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipients);

        // Act
        await _orchestrator.NotifyGoalCreatedAsync(goalId, organizationId);

        // Assert
        _repoMock.Verify(
            r => r.AddRangeAsync(
                It.Is<IEnumerable<Notification>>(n =>
                    n.Count() == 2 &&
                    n.All(x => x.Type == NotificationType.GoalCreated && x.Title == "Nova meta criada")),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyMissionCreatedAsync_WithEmptyRecipients_DoesNotCreateNotifications()
    {
        // Arrange
        var goalId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        _recipientResolverMock
            .Setup(r => r.ResolveGoalRecipientsAsync(goalId, organizationId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        // Act
        await _orchestrator.NotifyGoalCreatedAsync(goalId, organizationId);

        // Assert
        _repoMock.Verify(
            r => r.AddRangeAsync(It.IsAny<IEnumerable<Notification>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NotifyMissionUpdatedAsync_WithRecipients_CreatesNotifications()
    {
        // Arrange
        var goalId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var recipients = new List<Guid> { Guid.NewGuid() };

        _recipientResolverMock
            .Setup(r => r.ResolveGoalRecipientsAsync(goalId, organizationId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipients);

        // Act
        await _orchestrator.NotifyGoalUpdatedAsync(goalId, organizationId);

        // Assert
        _repoMock.Verify(
            r => r.AddRangeAsync(
                It.Is<IEnumerable<Notification>>(n =>
                    n.Count() == 1 &&
                    n.All(x => x.Type == NotificationType.GoalUpdated && x.Title == "Meta atualizada")),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyMissionDeletedAsync_WithRecipients_CreatesNotifications()
    {
        // Arrange
        var goalId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var recipients = new List<Guid> { Guid.NewGuid() };

        _recipientResolverMock
            .Setup(r => r.ResolveGoalRecipientsAsync(goalId, organizationId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipients);

        // Act
        await _orchestrator.NotifyGoalDeletedAsync(goalId, organizationId);

        // Assert
        _repoMock.Verify(
            r => r.AddRangeAsync(
                It.Is<IEnumerable<Notification>>(n =>
                    n.Count() == 1 &&
                    n.All(x => x.Type == NotificationType.GoalDeleted && x.Title == "Meta removida")),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyCheckinCreatedAsync_WithRecipients_CreatesNotifications()
    {
        // Arrange
        var checkinId = Guid.NewGuid();
        var missionMetricId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var excludeCollaboratorId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        var recipients = new List<Guid> { Guid.NewGuid() };

        _recipientResolverMock
            .Setup(r => r.ResolveGoalIdFromIndicatorAsync(missionMetricId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goalId);

        _recipientResolverMock
            .Setup(r => r.ResolveGoalRecipientsAsync(goalId, organizationId, excludeCollaboratorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipients);

        // Act
        await _orchestrator.NotifyCheckinCreatedAsync(checkinId, missionMetricId, organizationId, excludeCollaboratorId);

        // Assert
        _repoMock.Verify(
            r => r.AddRangeAsync(
                It.Is<IEnumerable<Notification>>(n =>
                    n.Count() == 1 &&
                    n.All(x => x.Type == NotificationType.CheckinCreated &&
                               x.Title == "Novo check-in registrado" &&
                               x.RelatedEntityId == checkinId &&
                               x.RelatedEntityType == "Checkin")),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyCheckinCreatedAsync_WhenMissionNotFound_DoesNotCreateNotifications()
    {
        // Arrange
        var checkinId = Guid.NewGuid();
        var missionMetricId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        _recipientResolverMock
            .Setup(r => r.ResolveGoalIdFromIndicatorAsync(missionMetricId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        // Act
        await _orchestrator.NotifyCheckinCreatedAsync(checkinId, missionMetricId, organizationId, null);

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
        var missionMetricId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var goalId = Guid.NewGuid();

        _recipientResolverMock
            .Setup(r => r.ResolveGoalIdFromIndicatorAsync(missionMetricId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goalId);

        _recipientResolverMock
            .Setup(r => r.ResolveGoalRecipientsAsync(goalId, organizationId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        // Act
        await _orchestrator.NotifyCheckinCreatedAsync(checkinId, missionMetricId, organizationId, null);

        // Assert
        _repoMock.Verify(
            r => r.AddRangeAsync(It.IsAny<IEnumerable<Notification>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
