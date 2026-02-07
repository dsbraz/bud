using Bud.Server.Application.Missions;
using Bud.Server.Services;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Missions;

public sealed class MissionQueryUseCaseTests
{
    [Fact]
    public async Task GetByIdAsync_DelegatesToService()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var missionService = new Mock<IMissionService>();
        var progressService = new Mock<IMissionProgressService>();
        missionService
            .Setup(s => s.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<Mission>.Success(new Mission { Id = missionId, Name = "M", OrganizationId = Guid.NewGuid() }));

        var useCase = new MissionQueryUseCase(missionService.Object, progressService.Object);

        // Act
        var result = await useCase.GetByIdAsync(missionId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(missionId);
    }

    [Fact]
    public async Task GetProgressAsync_DelegatesToProgressService()
    {
        // Arrange
        var ids = new List<Guid> { Guid.NewGuid() };
        var missionService = new Mock<IMissionService>();
        var progressService = new Mock<IMissionProgressService>();
        progressService
            .Setup(s => s.GetProgressAsync(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<List<MissionProgressDto>>.Success([]));

        var useCase = new MissionQueryUseCase(missionService.Object, progressService.Object);

        // Act
        var result = await useCase.GetProgressAsync(ids);

        // Assert
        result.IsSuccess.Should().BeTrue();
        progressService.Verify(s => s.GetProgressAsync(ids, It.IsAny<CancellationToken>()), Times.Once);
    }
}
