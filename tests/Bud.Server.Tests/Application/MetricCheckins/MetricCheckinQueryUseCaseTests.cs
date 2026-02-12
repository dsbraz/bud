using Bud.Server.Application.Abstractions;
using Bud.Server.Application.MetricCheckins;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.MetricCheckins;

public sealed class MetricCheckinQueryUseCaseTests
{
    [Fact]
    public async Task GetByIdAsync_DelegatesToService()
    {
        // Arrange
        var checkinId = Guid.NewGuid();
        var checkinService = new Mock<IMetricCheckinQueryService>();
        checkinService
            .Setup(s => s.GetByIdAsync(checkinId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<MetricCheckin>.Success(new MetricCheckin
            {
                Id = checkinId,
                MissionMetricId = Guid.NewGuid(),
                CollaboratorId = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid(),
                CheckinDate = DateTime.UtcNow,
                ConfidenceLevel = 3
            }));

        var useCase = new MetricCheckinQueryUseCase(checkinService.Object);

        // Act
        var result = await useCase.GetByIdAsync(checkinId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(checkinId);
    }

    [Fact]
    public async Task GetAllAsync_DelegatesToService()
    {
        // Arrange
        var checkinService = new Mock<IMetricCheckinQueryService>();
        checkinService
            .Setup(s => s.GetAllAsync(
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                1,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<PagedResult<MetricCheckin>>.Success(new PagedResult<MetricCheckin>()));

        var useCase = new MetricCheckinQueryUseCase(checkinService.Object);

        // Act
        var result = await useCase.GetAllAsync(null, null, 1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        checkinService.Verify(s => s.GetAllAsync(null, null, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }
}
