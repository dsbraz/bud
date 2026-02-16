using Bud.Server.Services;
using Bud.Server.Application.MissionObjectives;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.MissionObjectives;

public sealed class MissionObjectiveQueryUseCaseTests
{
    [Fact]
    public async Task GetByIdAsync_DelegatesToService()
    {
        var objectiveId = Guid.NewGuid();
        var objectiveService = new Mock<IMissionObjectiveService>();
        var progressService = new Mock<IMissionProgressService>();
        objectiveService
            .Setup(s => s.GetByIdAsync(objectiveId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<MissionObjective>.Success(
                MissionObjective.Create(objectiveId, Guid.NewGuid(), Guid.NewGuid(), "Obj", null)));

        var useCase = new MissionObjectiveQueryUseCase(objectiveService.Object, progressService.Object);

        var result = await useCase.GetByIdAsync(objectiveId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(objectiveId);
    }

    [Fact]
    public async Task GetByMissionAsync_DelegatesToService()
    {
        var missionId = Guid.NewGuid();
        var objectiveService = new Mock<IMissionObjectiveService>();
        var progressService = new Mock<IMissionProgressService>();
        objectiveService
            .Setup(s => s.GetByMissionAsync(missionId, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<PagedResult<MissionObjective>>.Success(
                new PagedResult<MissionObjective> { Items = [], Total = 0, Page = 1, PageSize = 10 }));

        var useCase = new MissionObjectiveQueryUseCase(objectiveService.Object, progressService.Object);

        var result = await useCase.GetByMissionAsync(missionId, null, 1, 10);

        result.IsSuccess.Should().BeTrue();
        objectiveService.Verify(s => s.GetByMissionAsync(missionId, null, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProgressAsync_DelegatesToProgressService()
    {
        var ids = new List<Guid> { Guid.NewGuid() };
        var objectiveService = new Mock<IMissionObjectiveService>();
        var progressService = new Mock<IMissionProgressService>();
        progressService
            .Setup(s => s.GetObjectiveProgressAsync(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<List<ObjectiveProgressDto>>.Success([]));

        var useCase = new MissionObjectiveQueryUseCase(objectiveService.Object, progressService.Object);

        var result = await useCase.GetProgressAsync(ids);

        result.IsSuccess.Should().BeTrue();
        progressService.Verify(s => s.GetObjectiveProgressAsync(ids, It.IsAny<CancellationToken>()), Times.Once);
    }
}
