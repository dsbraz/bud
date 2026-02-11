using Bud.Server.Application.Dashboard;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using FluentAssertions;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Bud.Server.Tests.UseCases;

public sealed class DashboardQueryUseCaseTests
{
    [Fact]
    public async Task GetMyDashboardAsync_WithoutCollaboratorInContext_ReturnsForbidden()
    {
        var dashboardService = new Mock<IDashboardService>();
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(x => x.CollaboratorId).Returns((Guid?)null);

        var useCase = new DashboardQueryUseCase(dashboardService.Object, tenantProvider.Object);
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await useCase.GetMyDashboardAsync(user);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Forbidden);
        result.Error.Should().Be("Colaborador não identificado.");
        dashboardService.Verify(s => s.GetMyDashboardAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetMyDashboardAsync_DelegatesToServiceUsingAuthenticatedCollaborator()
    {
        var collaboratorId = Guid.NewGuid();
        var dashboardService = new Mock<IDashboardService>();
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(x => x.CollaboratorId).Returns(collaboratorId);
        dashboardService
            .Setup(s => s.GetMyDashboardAsync(collaboratorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<MyDashboardResponse>.Success(new MyDashboardResponse()));

        var useCase = new DashboardQueryUseCase(dashboardService.Object, tenantProvider.Object);
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await useCase.GetMyDashboardAsync(user);

        result.IsSuccess.Should().BeTrue();
        dashboardService.Verify(s => s.GetMyDashboardAsync(collaboratorId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMyDashboardAsync_PropagatesNotFound()
    {
        var collaboratorId = Guid.NewGuid();
        var dashboardService = new Mock<IDashboardService>();
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(x => x.CollaboratorId).Returns(collaboratorId);
        dashboardService
            .Setup(s => s.GetMyDashboardAsync(collaboratorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<MyDashboardResponse>.NotFound("Colaborador não encontrado."));

        var useCase = new DashboardQueryUseCase(dashboardService.Object, tenantProvider.Object);
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await useCase.GetMyDashboardAsync(user);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Colaborador não encontrado.");
    }
}
