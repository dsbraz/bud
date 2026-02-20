using Bud.Server.Infrastructure.Services;
using Bud.Server.Infrastructure.Repositories;
using Bud.Server.Application.Common;
using Bud.Server.Application.MissionObjectives;
using Bud.Server.Domain.ReadModels;

using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.MissionObjectives;

public sealed class MissionObjectiveQueryUseCaseTests
{
    private readonly Mock<IMissionObjectiveRepository> _repo = new();
    private readonly Mock<IMissionProgressService> _progressService = new();

    private MissionObjectiveQueryUseCase CreateUseCase()
        => new(_repo.Object, _progressService.Object);

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsObjective()
    {
        var objectiveId = Guid.NewGuid();
        var objective = MissionObjective.Create(objectiveId, Guid.NewGuid(), Guid.NewGuid(), "Obj", null);

        _repo
            .Setup(r => r.GetByIdAsync(objectiveId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(objective);

        var useCase = CreateUseCase();

        var result = await useCase.GetByIdAsync(objectiveId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(objectiveId);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNotFound()
    {
        _repo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MissionObjective?)null);

        var useCase = CreateUseCase();

        var result = await useCase.GetByIdAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetByMissionAsync_ReturnsPagedResult()
    {
        var missionId = Guid.NewGuid();
        var pagedResult = new PagedResult<MissionObjective>
        {
            Items = [],
            Total = 0,
            Page = 1,
            PageSize = 10
        };

        _repo
            .Setup(r => r.GetByMissionAsync(missionId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var useCase = CreateUseCase();

        var result = await useCase.GetByMissionAsync(missionId, 1, 10);

        result.IsSuccess.Should().BeTrue();
        _repo.Verify(r => r.GetByMissionAsync(missionId, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProgressAsync_DelegatesToProgressService()
    {
        var ids = new List<Guid> { Guid.NewGuid() };
        _progressService
            .Setup(s => s.GetObjectiveProgressAsync(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<ObjectiveProgressSnapshot>>.Success([]));

        var useCase = CreateUseCase();

        var result = await useCase.GetProgressAsync(ids);

        result.IsSuccess.Should().BeTrue();
        _progressService.Verify(s => s.GetObjectiveProgressAsync(ids, It.IsAny<CancellationToken>()), Times.Once);
    }
}
