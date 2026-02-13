using Bud.Server.Services;
using Bud.Server.Application.Notifications;
using Bud.Server.MultiTenancy;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.UseCases;

public sealed class NotificationCommandUseCaseTests
{
    [Fact]
    public async Task MarkAsReadAsync_WithoutCollaborator_ReturnsForbidden()
    {
        var service = new Mock<INotificationService>();
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(x => x.CollaboratorId).Returns((Guid?)null);

        var useCase = new NotificationCommandUseCase(service.Object, tenantProvider.Object);

        var result = await useCase.MarkAsReadAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Forbidden);
        result.Error.Should().Be("Colaborador não identificado.");
    }

    [Fact]
    public async Task MarkAsReadAsync_DelegatesToService()
    {
        var collaboratorId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var service = new Mock<INotificationService>();
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(x => x.CollaboratorId).Returns(collaboratorId);
        service
            .Setup(s => s.MarkAsReadAsync(notificationId, collaboratorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Success());

        var useCase = new NotificationCommandUseCase(service.Object, tenantProvider.Object);

        var result = await useCase.MarkAsReadAsync(notificationId);

        result.IsSuccess.Should().BeTrue();
        service.Verify(s => s.MarkAsReadAsync(notificationId, collaboratorId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_WithoutCollaborator_ReturnsForbidden()
    {
        var service = new Mock<INotificationService>();
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(x => x.CollaboratorId).Returns((Guid?)null);

        var useCase = new NotificationCommandUseCase(service.Object, tenantProvider.Object);

        var result = await useCase.MarkAllAsReadAsync();

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Forbidden);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_DelegatesToService()
    {
        var collaboratorId = Guid.NewGuid();
        var service = new Mock<INotificationService>();
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(x => x.CollaboratorId).Returns(collaboratorId);
        service
            .Setup(s => s.MarkAllAsReadAsync(collaboratorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Success());

        var useCase = new NotificationCommandUseCase(service.Object, tenantProvider.Object);

        var result = await useCase.MarkAllAsReadAsync();

        result.IsSuccess.Should().BeTrue();
        service.Verify(s => s.MarkAllAsReadAsync(collaboratorId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
