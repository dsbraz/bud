using Bud.Server.Application.Common;
using Bud.Server.Application.Objectives;
using Bud.Server.Domain.ReadModels;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Application.Ports;
using Bud.Shared.Contracts;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Objectives;

public sealed class MissionObjectiveReadUseCasesTests
{
    private readonly Mock<IObjectiveRepository> _repository = new();
    private readonly Mock<IMissionProgressService> _progressService = new();

    [Fact]
    public async Task ViewMissionObjectiveDetails_WhenFound_ReturnsObjective()
    {
        var objectiveId = Guid.NewGuid();
        var objective = Objective.Create(objectiveId, Guid.NewGuid(), Guid.NewGuid(), "Obj", null);

        _repository
            .Setup(repository => repository.GetByIdAsync(objectiveId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(objective);

        var useCase = new GetObjectiveById(_repository.Object);

        var result = await useCase.ExecuteAsync(objectiveId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(objectiveId);
    }

    [Fact]
    public async Task ViewMissionObjectiveDetails_WhenNotFound_ReturnsNotFound()
    {
        _repository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Objective?)null);

        var useCase = new GetObjectiveById(_repository.Object);

        var result = await useCase.ExecuteAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task ListMissionObjectives_ReturnsPagedResult()
    {
        var missionId = Guid.NewGuid();

        var pagedResult = new PagedResult<Objective>
        {
            Items = [],
            Total = 0,
            Page = 1,
            PageSize = 10
        };

        _repository
            .Setup(repository => repository.GetAllAsync(missionId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var useCase = new ListObjectives(_repository.Object);

        var result = await useCase.ExecuteAsync(missionId, 1, 10);

        result.IsSuccess.Should().BeTrue();
        _repository.Verify(repository => repository.GetAllAsync(missionId, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CalculateMissionObjectiveProgress_DelegatesToProgressService()
    {
        var objectiveIds = new List<Guid> { Guid.NewGuid() };

        _progressService
            .Setup(service => service.GetObjectiveProgressAsync(objectiveIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<ObjectiveProgressSnapshot>>.Success([]));

        var useCase = new ListObjectiveProgress(_progressService.Object);

        var result = await useCase.ExecuteAsync(objectiveIds);

        result.IsSuccess.Should().BeTrue();
        _progressService.Verify(service => service.GetObjectiveProgressAsync(objectiveIds, It.IsAny<CancellationToken>()), Times.Once);
    }
}
