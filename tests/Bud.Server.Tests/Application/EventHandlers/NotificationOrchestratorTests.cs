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
        var missionId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var recipients = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        _recipientResolverMock
            .Setup(r => r.ResolveMissionRecipientsAsync(missionId, organizationId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipients);

        // Act
        await _orchestrator.NotifyMissionCreatedAsync(missionId, organizationId);

        // Assert
        _repoMock.Verify(
            r => r.AddRangeAsync(
                It.Is<IEnumerable<NotificationResponse>>(n =>
                    n.Count() == 2 &&
                    n.All(x => x.Type == NotificationType.MissionCreated && x.Title == "Nova missão criada")),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyMissionCreatedAsync_WithEmptyRecipients_DoesNotCreateNotifications()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        _recipientResolverMock
            .Setup(r => r.ResolveMissionRecipientsAsync(missionId, organizationId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        // Act
        await _orchestrator.NotifyMissionCreatedAsync(missionId, organizationId);

        // Assert
        _repoMock.Verify(
            r => r.AddRangeAsync(It.IsAny<IEnumerable<NotificationResponse>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NotifyMissionUpdatedAsync_WithRecipients_CreatesNotifications()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var recipients = new List<Guid> { Guid.NewGuid() };

        _recipientResolverMock
            .Setup(r => r.ResolveMissionRecipientsAsync(missionId, organizationId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipients);

        // Act
        await _orchestrator.NotifyMissionUpdatedAsync(missionId, organizationId);

        // Assert
        _repoMock.Verify(
            r => r.AddRangeAsync(
                It.Is<IEnumerable<NotificationResponse>>(n =>
                    n.Count() == 1 &&
                    n.All(x => x.Type == NotificationType.MissionUpdated && x.Title == "Missão atualizada")),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyMissionDeletedAsync_WithRecipients_CreatesNotifications()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var recipients = new List<Guid> { Guid.NewGuid() };

        _recipientResolverMock
            .Setup(r => r.ResolveMissionRecipientsAsync(missionId, organizationId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipients);

        // Act
        await _orchestrator.NotifyMissionDeletedAsync(missionId, organizationId);

        // Assert
        _repoMock.Verify(
            r => r.AddRangeAsync(
                It.Is<IEnumerable<NotificationResponse>>(n =>
                    n.Count() == 1 &&
                    n.All(x => x.Type == NotificationType.MissionDeleted && x.Title == "Missão removida")),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyMetricCheckinCreatedAsync_WithRecipients_CreatesNotifications()
    {
        // Arrange
        var checkinId = Guid.NewGuid();
        var missionMetricId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var excludeCollaboratorId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var recipients = new List<Guid> { Guid.NewGuid() };

        _recipientResolverMock
            .Setup(r => r.ResolveMissionIdFromMetricAsync(missionMetricId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(missionId);

        _recipientResolverMock
            .Setup(r => r.ResolveMissionRecipientsAsync(missionId, organizationId, excludeCollaboratorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipients);

        // Act
        await _orchestrator.NotifyMetricCheckinCreatedAsync(checkinId, missionMetricId, organizationId, excludeCollaboratorId);

        // Assert
        _repoMock.Verify(
            r => r.AddRangeAsync(
                It.Is<IEnumerable<NotificationResponse>>(n =>
                    n.Count() == 1 &&
                    n.All(x => x.Type == NotificationType.MetricCheckinCreated &&
                               x.Title == "Novo check-in registrado" &&
                               x.RelatedEntityId == checkinId &&
                               x.RelatedEntityType == "MetricCheckin")),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyMetricCheckinCreatedAsync_WhenMissionNotFound_DoesNotCreateNotifications()
    {
        // Arrange
        var checkinId = Guid.NewGuid();
        var missionMetricId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        _recipientResolverMock
            .Setup(r => r.ResolveMissionIdFromMetricAsync(missionMetricId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        // Act
        await _orchestrator.NotifyMetricCheckinCreatedAsync(checkinId, missionMetricId, organizationId, null);

        // Assert
        _recipientResolverMock.Verify(
            r => r.ResolveMissionRecipientsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _repoMock.Verify(
            r => r.AddRangeAsync(It.IsAny<IEnumerable<NotificationResponse>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task NotifyMetricCheckinCreatedAsync_WithEmptyRecipients_DoesNotCreateNotifications()
    {
        // Arrange
        var checkinId = Guid.NewGuid();
        var missionMetricId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        _recipientResolverMock
            .Setup(r => r.ResolveMissionIdFromMetricAsync(missionMetricId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(missionId);

        _recipientResolverMock
            .Setup(r => r.ResolveMissionRecipientsAsync(missionId, organizationId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        // Act
        await _orchestrator.NotifyMetricCheckinCreatedAsync(checkinId, missionMetricId, organizationId, null);

        // Assert
        _repoMock.Verify(
            r => r.AddRangeAsync(It.IsAny<IEnumerable<NotificationResponse>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
