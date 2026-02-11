using Bud.Server.Application.Abstractions;
using Bud.Server.Application.Common.Results;
using Bud.Server.Application.Notifications.Events;
using Bud.Server.Domain.MetricCheckins.Events;
using Bud.Shared.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Notifications.Events;

public sealed class MetricCheckinCreatedNotificationHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithRecipients_CreatesNotifications()
    {
        // Arrange
        var metricCheckinId = Guid.NewGuid();
        var missionMetricId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var collaboratorId = Guid.NewGuid();
        var recipientIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        var notificationService = new Mock<INotificationService>();
        notificationService
            .Setup(s => s.CreateForMultipleRecipientsAsync(
                recipientIds, orgId, It.IsAny<string>(), It.IsAny<string>(),
                NotificationType.MetricCheckinCreated, metricCheckinId, "MetricCheckin",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Success());

        var recipientResolver = new Mock<INotificationRecipientResolver>();
        recipientResolver
            .Setup(r => r.ResolveMissionIdFromMetricAsync(missionMetricId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(missionId);
        recipientResolver
            .Setup(r => r.ResolveMissionRecipientsAsync(missionId, orgId, collaboratorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipientIds);

        var logger = new Mock<ILogger<MetricCheckinCreatedNotificationHandler>>();
        var handler = new MetricCheckinCreatedNotificationHandler(
            notificationService.Object, recipientResolver.Object, logger.Object);

        // Act
        await handler.HandleAsync(new MetricCheckinCreatedDomainEvent(metricCheckinId, missionMetricId, orgId, collaboratorId));

        // Assert
        notificationService.Verify(s => s.CreateForMultipleRecipientsAsync(
            recipientIds, orgId,
            It.IsAny<string>(),
            It.IsAny<string>(),
            NotificationType.MetricCheckinCreated,
            metricCheckinId, "MetricCheckin",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_MetricNotFound_DoesNotCreateNotifications()
    {
        // Arrange
        var missionMetricId = Guid.NewGuid();

        var notificationService = new Mock<INotificationService>();
        var recipientResolver = new Mock<INotificationRecipientResolver>();
        recipientResolver
            .Setup(r => r.ResolveMissionIdFromMetricAsync(missionMetricId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var logger = new Mock<ILogger<MetricCheckinCreatedNotificationHandler>>();
        var handler = new MetricCheckinCreatedNotificationHandler(
            notificationService.Object, recipientResolver.Object, logger.Object);

        // Act
        await handler.HandleAsync(new MetricCheckinCreatedDomainEvent(
            Guid.NewGuid(), missionMetricId, Guid.NewGuid(), Guid.NewGuid()));

        // Assert
        notificationService.Verify(s => s.CreateForMultipleRecipientsAsync(
            It.IsAny<IEnumerable<Guid>>(), It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<Guid?>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NoRecipients_DoesNotCreateNotifications()
    {
        // Arrange
        var missionMetricId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var collaboratorId = Guid.NewGuid();

        var notificationService = new Mock<INotificationService>();
        var recipientResolver = new Mock<INotificationRecipientResolver>();
        recipientResolver
            .Setup(r => r.ResolveMissionIdFromMetricAsync(missionMetricId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(missionId);
        recipientResolver
            .Setup(r => r.ResolveMissionRecipientsAsync(missionId, orgId, collaboratorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        var logger = new Mock<ILogger<MetricCheckinCreatedNotificationHandler>>();
        var handler = new MetricCheckinCreatedNotificationHandler(
            notificationService.Object, recipientResolver.Object, logger.Object);

        // Act
        await handler.HandleAsync(new MetricCheckinCreatedDomainEvent(
            Guid.NewGuid(), missionMetricId, orgId, collaboratorId));

        // Assert
        notificationService.Verify(s => s.CreateForMultipleRecipientsAsync(
            It.IsAny<IEnumerable<Guid>>(), It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<Guid?>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
