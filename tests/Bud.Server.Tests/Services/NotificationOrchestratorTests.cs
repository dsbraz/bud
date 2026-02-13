using Bud.Server.Services;
using Bud.Shared.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Services;

public class NotificationOrchestratorTests
{
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<INotificationRecipientResolver> _recipientResolverMock;
    private readonly NotificationOrchestrator _orchestrator;

    public NotificationOrchestratorTests()
    {
        _notificationServiceMock = new Mock<INotificationService>();
        _recipientResolverMock = new Mock<INotificationRecipientResolver>();
        _orchestrator = new NotificationOrchestrator(
            _notificationServiceMock.Object,
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

        _notificationServiceMock
            .Setup(s => s.CreateForMultipleRecipientsAsync(
                recipients,
                organizationId,
                It.IsAny<string>(),
                It.IsAny<string>(),
                NotificationType.MissionCreated,
                missionId,
                "Mission",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Success());

        // Act
        await _orchestrator.NotifyMissionCreatedAsync(missionId, organizationId);

        // Assert
        _notificationServiceMock.Verify(
            s => s.CreateForMultipleRecipientsAsync(
                recipients,
                organizationId,
                "Nova missão criada",
                "Uma nova missão foi criada na sua organização.",
                NotificationType.MissionCreated,
                missionId,
                "Mission",
                It.IsAny<CancellationToken>()),
            Times.Once);
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
        _notificationServiceMock.Verify(
            s => s.CreateForMultipleRecipientsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationType>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
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

        _notificationServiceMock
            .Setup(s => s.CreateForMultipleRecipientsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationType>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Success());

        // Act
        await _orchestrator.NotifyMissionUpdatedAsync(missionId, organizationId);

        // Assert
        _notificationServiceMock.Verify(
            s => s.CreateForMultipleRecipientsAsync(
                recipients,
                organizationId,
                "Missão atualizada",
                "Uma missão foi atualizada na sua organização.",
                NotificationType.MissionUpdated,
                missionId,
                "Mission",
                It.IsAny<CancellationToken>()),
            Times.Once);
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

        _notificationServiceMock
            .Setup(s => s.CreateForMultipleRecipientsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationType>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Success());

        // Act
        await _orchestrator.NotifyMissionDeletedAsync(missionId, organizationId);

        // Assert
        _notificationServiceMock.Verify(
            s => s.CreateForMultipleRecipientsAsync(
                recipients,
                organizationId,
                "Missão removida",
                "Uma missão foi removida da sua organização.",
                NotificationType.MissionDeleted,
                missionId,
                "Mission",
                It.IsAny<CancellationToken>()),
            Times.Once);
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

        _notificationServiceMock
            .Setup(s => s.CreateForMultipleRecipientsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationType>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Success());

        // Act
        await _orchestrator.NotifyMetricCheckinCreatedAsync(checkinId, missionMetricId, organizationId, excludeCollaboratorId);

        // Assert
        _notificationServiceMock.Verify(
            s => s.CreateForMultipleRecipientsAsync(
                recipients,
                organizationId,
                "Novo check-in registrado",
                "Um novo check-in de métrica foi registrado.",
                NotificationType.MetricCheckinCreated,
                checkinId,
                "MetricCheckin",
                It.IsAny<CancellationToken>()),
            Times.Once);
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

        _notificationServiceMock.Verify(
            s => s.CreateForMultipleRecipientsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationType>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
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
        _notificationServiceMock.Verify(
            s => s.CreateForMultipleRecipientsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationType>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
