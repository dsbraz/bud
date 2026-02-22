using Bud.Server.Domain.Repositories;
using Bud.Server.Application.Common;
using Bud.Server.Application.Notifications;
using Bud.Server.MultiTenancy;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Notifications;

public sealed class NotificationWriteUseCasesTests
{
    private readonly Mock<INotificationRepository> _repo = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();

    private PatchNotification CreateMarkAsReadUseCase()
        => new(_repo.Object, _tenantProvider.Object);

    private PatchNotifications CreateMarkAllAsReadUseCase()
        => new(_repo.Object, _tenantProvider.Object);

    #region MarkAsReadAsync

    [Fact]
    public async Task MarkAsReadAsync_WithoutCollaborator_ReturnsForbidden()
    {
        _tenantProvider.SetupGet(x => x.CollaboratorId).Returns((Guid?)null);

        var result = await CreateMarkAsReadUseCase().ExecuteAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        result.Error.Should().Be("Colaborador não identificado.");
    }

    [Fact]
    public async Task MarkAsReadAsync_WithNonExistentNotification_ReturnsNotFound()
    {
        var collaboratorId = Guid.NewGuid();
        _tenantProvider.SetupGet(x => x.CollaboratorId).Returns(collaboratorId);
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification?)null);

        var result = await CreateMarkAsReadUseCase().ExecuteAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Notificação não encontrada.");
    }

    [Fact]
    public async Task MarkAsReadAsync_WithDifferentRecipient_ReturnsForbidden()
    {
        var collaboratorId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        _tenantProvider.SetupGet(x => x.CollaboratorId).Returns(collaboratorId);

        var notification = new Notification
        {
            Id = notificationId,
            RecipientCollaboratorId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Title = "Test",
            Message = "Test",
            Type = NotificationType.MissionCreated,
            CreatedAtUtc = DateTime.UtcNow
        };
        _repo.Setup(r => r.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        var result = await CreateMarkAsReadUseCase().ExecuteAsync(notificationId);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        result.Error.Should().Be("Você não tem permissão para marcar esta notificação como lida.");
    }

    [Fact]
    public async Task MarkAsReadAsync_AlreadyRead_ReturnsSuccessWithoutSaving()
    {
        var collaboratorId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        _tenantProvider.SetupGet(x => x.CollaboratorId).Returns(collaboratorId);

        var notification = new Notification
        {
            Id = notificationId,
            RecipientCollaboratorId = collaboratorId,
            OrganizationId = Guid.NewGuid(),
            Title = "Test",
            Message = "Test",
            Type = NotificationType.MissionCreated,
            IsRead = true,
            ReadAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };
        _repo.Setup(r => r.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        var result = await CreateMarkAsReadUseCase().ExecuteAsync(notificationId);

        result.IsSuccess.Should().BeTrue();
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MarkAsReadAsync_WithValidNotification_MarksAndSaves()
    {
        var collaboratorId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        _tenantProvider.SetupGet(x => x.CollaboratorId).Returns(collaboratorId);

        var notification = new Notification
        {
            Id = notificationId,
            RecipientCollaboratorId = collaboratorId,
            OrganizationId = Guid.NewGuid(),
            Title = "Test",
            Message = "Test",
            Type = NotificationType.MissionCreated,
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow
        };
        _repo.Setup(r => r.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        var result = await CreateMarkAsReadUseCase().ExecuteAsync(notificationId);

        result.IsSuccess.Should().BeTrue();
        notification.IsRead.Should().BeTrue();
        notification.ReadAtUtc.Should().NotBeNull();
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region MarkAllAsReadAsync

    [Fact]
    public async Task MarkAllAsReadAsync_WithoutCollaborator_ReturnsForbidden()
    {
        _tenantProvider.SetupGet(x => x.CollaboratorId).Returns((Guid?)null);

        var result = await CreateMarkAllAsReadUseCase().ExecuteAsync();

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_DelegatesToRepository()
    {
        var collaboratorId = Guid.NewGuid();
        _tenantProvider.SetupGet(x => x.CollaboratorId).Returns(collaboratorId);

        var result = await CreateMarkAllAsReadUseCase().ExecuteAsync();

        result.IsSuccess.Should().BeTrue();
        _repo.Verify(r => r.MarkAllAsReadAsync(collaboratorId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
