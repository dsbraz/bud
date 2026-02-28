using Bud.Server.Domain.Repositories;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Application.UseCases.Checkins;
using Bud.Shared.Contracts;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Checkins;

public sealed class MetricCheckinReadUseCasesTests
{
    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsSuccess()
    {
        // Arrange
        var indicatorId = Guid.NewGuid();
        var checkinId = Guid.NewGuid();
        var checkin = new Checkin
        {
            Id = checkinId,
            IndicatorId = indicatorId,
            CollaboratorId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        var checkinRepository = new Mock<IIndicatorRepository>();
        checkinRepository
            .Setup(r => r.GetCheckinByIdAsync(checkinId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(checkin);

        var useCase = new GetCheckinById(checkinRepository.Object);

        // Act
        var result = await useCase.ExecuteAsync(indicatorId, checkinId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(checkinId);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var indicatorId = Guid.NewGuid();
        var checkinId = Guid.NewGuid();
        var checkinRepository = new Mock<IIndicatorRepository>();
        checkinRepository
            .Setup(r => r.GetCheckinByIdAsync(checkinId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Checkin?)null);

        var useCase = new GetCheckinById(checkinRepository.Object);

        // Act
        var result = await useCase.ExecuteAsync(indicatorId, checkinId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Check-in não encontrado.");
    }

    [Fact]
    public async Task GetAllAsync_DelegatesToRepository()
    {
        // Arrange
        var checkinRepository = new Mock<IIndicatorRepository>();
        checkinRepository
            .Setup(r => r.GetCheckinsAsync(
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                1,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Checkin>());

        var useCase = new ListCheckins(checkinRepository.Object);
        var indicatorId = Guid.NewGuid();

        // Act
        var result = await useCase.ExecuteAsync(indicatorId, 1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        checkinRepository.Verify(r => r.GetCheckinsAsync(indicatorId, null, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }
}
