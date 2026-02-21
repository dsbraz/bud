using Bud.Server.Application.Common;
using Bud.Server.Application.MissionObjectives;
using Bud.Server.Application.Projections;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Infrastructure.Services;
using Bud.Shared.Contracts;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.MissionObjectives;

public sealed class MissionObjectiveReadUseCasesTests
{
    private readonly Mock<IMissionObjectiveRepository> _repository = new();
    private readonly Mock<IMissionProgressService> _progressService = new();

    [Fact]
    public async Task ViewMissionObjectiveDetails_WhenFound_ReturnsObjective()
    {
        var objectiveId = Guid.NewGuid();
        var objective = MissionObjective.Create(objectiveId, Guid.NewGuid(), Guid.NewGuid(), "Obj", null);

        _repository
            .Setup(repository => repository.GetByIdAsync(objectiveId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(objective);

        var useCase = new ViewMissionObjectiveDetails(_repository.Object);

        var result = await useCase.ExecuteAsync(objectiveId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(objectiveId);
    }

    [Fact]
    public async Task ViewMissionObjectiveDetails_WhenNotFound_ReturnsNotFound()
    {
        _repository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MissionObjective?)null);

        var useCase = new ViewMissionObjectiveDetails(_repository.Object);

        var result = await useCase.ExecuteAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task ListMissionObjectives_ReturnsPagedResult()
    {
        var missionId = Guid.NewGuid();

        var pagedResult = new PagedResult<MissionObjective>
        {
            Items = [],
            Total = 0,
            Page = 1,
            PageSize = 10
        };

        _repository
            .Setup(repository => repository.GetByMissionAsync(missionId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var useCase = new ListMissionObjectives(_repository.Object);

        var result = await useCase.ExecuteAsync(missionId, 1, 10);

        result.IsSuccess.Should().BeTrue();
        _repository.Verify(repository => repository.GetByMissionAsync(missionId, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CalculateMissionObjectiveProgress_DelegatesToProgressService()
    {
        var objectiveIds = new List<Guid> { Guid.NewGuid() };

        _progressService
            .Setup(service => service.GetObjectiveProgressAsync(objectiveIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<ObjectiveProgressSnapshot>>.Success([]));

        var useCase = new CalculateMissionObjectiveProgress(_progressService.Object);

        var result = await useCase.ExecuteAsync(objectiveIds);

        result.IsSuccess.Should().BeTrue();
        _progressService.Verify(service => service.GetObjectiveProgressAsync(objectiveIds, It.IsAny<CancellationToken>()), Times.Once);
    }
}
