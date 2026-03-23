using FluentAssertions;
using Moq;
using System.Security.Claims;
using Xunit;
using Bud.Application.Common;

namespace Bud.Application.UnitTests.Application.Me;

public sealed class GetMyDashboardTests
{
    [Fact]
    public async Task ExecuteAsync_WithoutCollaboratorInContext_ReturnsForbidden()
    {
        var repository = new Mock<IMyDashboardReadStore>();
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(x => x.CollaboratorId).Returns((Guid?)null);

        var useCase = new GetMyDashboard(repository.Object, tenantProvider.Object);
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await useCase.ExecuteAsync(user);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        result.Error.Should().Be("Colaborador não identificado.");
        repository.Verify(r => r.GetMyDashboardAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_DelegatesToRepositoryUsingAuthenticatedCollaborator()
    {
        var collaboratorId = Guid.NewGuid();
        var repository = new Mock<IMyDashboardReadStore>();
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(x => x.CollaboratorId).Returns(collaboratorId);
        repository
            .Setup(r => r.GetMyDashboardAsync(collaboratorId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardSnapshot());

        var useCase = new GetMyDashboard(repository.Object, tenantProvider.Object);
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await useCase.ExecuteAsync(user);

        result.IsSuccess.Should().BeTrue();
        repository.Verify(r => r.GetMyDashboardAsync(collaboratorId, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_RepositoryReturnsNull_ReturnsNotFound()
    {
        var collaboratorId = Guid.NewGuid();
        var repository = new Mock<IMyDashboardReadStore>();
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(x => x.CollaboratorId).Returns(collaboratorId);
        repository
            .Setup(r => r.GetMyDashboardAsync(collaboratorId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DashboardSnapshot?)null);

        var useCase = new GetMyDashboard(repository.Object, tenantProvider.Object);
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await useCase.ExecuteAsync(user);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Colaborador não encontrado.");
    }

    [Fact]
    public async Task ExecuteAsync_WithTeamId_PassesTeamIdToRepository()
    {
        var collaboratorId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var repository = new Mock<IMyDashboardReadStore>();
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(x => x.CollaboratorId).Returns(collaboratorId);
        repository
            .Setup(r => r.GetMyDashboardAsync(collaboratorId, teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardSnapshot());

        var useCase = new GetMyDashboard(repository.Object, tenantProvider.Object);
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await useCase.ExecuteAsync(user, teamId);

        result.IsSuccess.Should().BeTrue();
        repository.Verify(r => r.GetMyDashboardAsync(collaboratorId, teamId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
