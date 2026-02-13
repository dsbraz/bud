using Bud.Server.Services;
using Bud.Server.Application.Notifications;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.UseCases;

public sealed class NotificationQueryUseCaseTests
{
    [Fact]
    public async Task GetMyNotificationsAsync_WithoutCollaborator_ReturnsForbidden()
    {
        var service = new Mock<INotificationService>();
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(x => x.CollaboratorId).Returns((Guid?)null);

        var useCase = new NotificationQueryUseCase(service.Object, tenantProvider.Object);

        var result = await useCase.GetMyNotificationsAsync(1, 10);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Forbidden);
        result.Error.Should().Be("Colaborador não identificado.");
        service.Verify(s => s.GetByRecipientAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetMyNotificationsAsync_DelegatesToService()
    {
        var collaboratorId = Guid.NewGuid();
        var service = new Mock<INotificationService>();
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(x => x.CollaboratorId).Returns(collaboratorId);
        service
            .Setup(s => s.GetByRecipientAsync(collaboratorId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<PagedResult<NotificationDto>>.Success(new PagedResult<NotificationDto>()));

        var useCase = new NotificationQueryUseCase(service.Object, tenantProvider.Object);

        var result = await useCase.GetMyNotificationsAsync(1, 10);

        result.IsSuccess.Should().BeTrue();
        service.Verify(s => s.GetByRecipientAsync(collaboratorId, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUnreadCountAsync_WithoutCollaborator_ReturnsForbidden()
    {
        var service = new Mock<INotificationService>();
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(x => x.CollaboratorId).Returns((Guid?)null);

        var useCase = new NotificationQueryUseCase(service.Object, tenantProvider.Object);

        var result = await useCase.GetUnreadCountAsync();

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Forbidden);
    }

    [Fact]
    public async Task GetUnreadCountAsync_DelegatesToService()
    {
        var collaboratorId = Guid.NewGuid();
        var service = new Mock<INotificationService>();
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(x => x.CollaboratorId).Returns(collaboratorId);
        service
            .Setup(s => s.GetUnreadCountAsync(collaboratorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<int>.Success(5));

        var useCase = new NotificationQueryUseCase(service.Object, tenantProvider.Object);

        var result = await useCase.GetUnreadCountAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(5);
    }
}
