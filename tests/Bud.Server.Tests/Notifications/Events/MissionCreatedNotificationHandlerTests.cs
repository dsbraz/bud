using Bud.Server.Application.Abstractions;
using Bud.Server.Application.Notifications.Events;
using Bud.Server.Domain.Missions.Events;
using Bud.Shared.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Notifications.Events;

public sealed class MissionCreatedNotificationHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithRecipients_CreatesNotifications()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var recipientIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        var notificationService = new Mock<INotificationService>();
        notificationService
            .Setup(s => s.CreateForMultipleRecipientsAsync(
                recipientIds, orgId, It.IsAny<string>(), It.IsAny<string>(),
                NotificationType.MissionCreated, missionId, "Mission",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Success());

        var recipientResolver = new Mock<INotificationRecipientResolver>();
        recipientResolver
            .Setup(r => r.ResolveMissionRecipientsAsync(missionId, orgId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipientIds);

        var logger = new Mock<ILogger<MissionCreatedNotificationHandler>>();
        var handler = new MissionCreatedNotificationHandler(notificationService.Object, recipientResolver.Object, logger.Object);

        // Act
        await handler.HandleAsync(new MissionCreatedDomainEvent(missionId, orgId));

        // Assert
        notificationService.Verify(s => s.CreateForMultipleRecipientsAsync(
            recipientIds, orgId,
            It.Is<string>(t => t.Contains("missão")),
            It.IsAny<string>(),
            NotificationType.MissionCreated,
            missionId, "Mission",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNoRecipients_DoesNotCreateNotifications()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        var notificationService = new Mock<INotificationService>();
        var recipientResolver = new Mock<INotificationRecipientResolver>();
        recipientResolver
            .Setup(r => r.ResolveMissionRecipientsAsync(missionId, orgId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        var logger = new Mock<ILogger<MissionCreatedNotificationHandler>>();
        var handler = new MissionCreatedNotificationHandler(notificationService.Object, recipientResolver.Object, logger.Object);

        // Act
        await handler.HandleAsync(new MissionCreatedDomainEvent(missionId, orgId));

        // Assert
        notificationService.Verify(s => s.CreateForMultipleRecipientsAsync(
            It.IsAny<IEnumerable<Guid>>(), It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<Guid?>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
