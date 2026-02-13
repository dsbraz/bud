using System.Security.Claims;
using Bud.Server.Authorization;
using Bud.Server.Data;
using Bud.Server.Services;
using Bud.Server.Application.MissionMetrics;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.MissionMetrics;

public sealed class MissionMetricCommandUseCaseTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));

    [Fact]
    public async Task CreateAsync_WhenMissionNotFound_ReturnsNotFound()
    {
        var metricService = new Mock<IMissionMetricService>(MockBehavior.Strict);
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>(MockBehavior.Strict);
        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetMissionAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mission?)null);

        var useCase = new MissionMetricCommandUseCase(metricService.Object, authorizationGateway.Object, entityLookup.Object);

        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Métrica",
            Type = MetricType.Qualitative,
            TargetText = "Descrição"
        };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Missão não encontrada.");
        metricService.VerifyNoOtherCalls();
        authorizationGateway.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var orgId = Guid.NewGuid();
        var metric = new MissionMetric
        {
            Id = Guid.NewGuid(),
            Name = "Métrica",
            Type = MetricType.Qualitative,
            MissionId = Guid.NewGuid(),
            OrganizationId = orgId
        };

        var metricService = new Mock<IMissionMetricService>(MockBehavior.Strict);
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetMissionMetricAsync(metric.Id, true, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);

        var useCase = new MissionMetricCommandUseCase(metricService.Object, authorizationGateway.Object, entityLookup.Object);

        var request = new UpdateMissionMetricRequest
        {
            Name = "Nova Métrica",
            Type = MetricType.Qualitative,
            TargetText = "Descrição"
        };

        var result = await useCase.UpdateAsync(User, metric.Id, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Forbidden);
        metricService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsync_WhenAuthorized_DelegatesToService()
    {
        var metric = new MissionMetric
        {
            Id = Guid.NewGuid(),
            Name = "Métrica",
            Type = MetricType.Qualitative,
            MissionId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid()
        };

        var metricService = new Mock<IMissionMetricService>();
        metricService.Setup(s => s.DeleteAsync(metric.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Success());

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, metric.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetMissionMetricAsync(metric.Id, true, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);

        var useCase = new MissionMetricCommandUseCase(metricService.Object, authorizationGateway.Object, entityLookup.Object);

        var result = await useCase.DeleteAsync(User, metric.Id);

        result.IsSuccess.Should().BeTrue();
        metricService.Verify(s => s.DeleteAsync(metric.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

}
