using Bud.Server.Application.Common;
using Bud.Server.Application.Notifications;
using Bud.Server.Application.Ports;
using Bud.Server.Domain.ReadModels;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Notifications;

public sealed class NotificationQueryUseCaseTests
{
    private readonly Mock<INotificationRepository> _repo = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();

    private NotificationQueryUseCase CreateUseCase()
        => new(_repo.Object, _tenantProvider.Object);

    #region GetMyNotificationsAsync

    [Fact]
    public async Task GetMyNotificationsAsync_WithoutCollaborator_ReturnsForbidden()
    {
        _tenantProvider.SetupGet(x => x.CollaboratorId).Returns((Guid?)null);

        var result = await CreateUseCase().GetMyNotificationsAsync(1, 10);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        result.Error.Should().Be("Colaborador não identificado.");
        _repo.Verify(r => r.GetByRecipientAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetMyNotificationsAsync_ReturnsPagedNotifications()
    {
        var collaboratorId = Guid.NewGuid();
        _tenantProvider.SetupGet(x => x.CollaboratorId).Returns(collaboratorId);

        var pagedResult = new PagedResult<NotificationSummary>
        {
            Items = [new NotificationSummary { Id = Guid.NewGuid(), Title = "Test", Message = "Msg", Type = "MissionCreated" }],
            Total = 1,
            Page = 1,
            PageSize = 10
        };
        _repo.Setup(r => r.GetByRecipientAsync(collaboratorId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await CreateUseCase().GetMyNotificationsAsync(1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Total.Should().Be(1);
        _repo.Verify(r => r.GetByRecipientAsync(collaboratorId, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetUnreadCountAsync

    [Fact]
    public async Task GetUnreadCountAsync_WithoutCollaborator_ReturnsForbidden()
    {
        _tenantProvider.SetupGet(x => x.CollaboratorId).Returns((Guid?)null);

        var result = await CreateUseCase().GetUnreadCountAsync();

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task GetUnreadCountAsync_ReturnsCount()
    {
        var collaboratorId = Guid.NewGuid();
        _tenantProvider.SetupGet(x => x.CollaboratorId).Returns(collaboratorId);
        _repo.Setup(r => r.GetUnreadCountAsync(collaboratorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var result = await CreateUseCase().GetUnreadCountAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(5);
    }

    #endregion
}
