using Bud.Application.Common;
using Bud.Application.Mapping;
using Bud.Application.UseCases.Goals;
using Bud.Application.ReadModels;
using Bud.Domain.Model;
using Bud.Domain.Repositories;
using Bud.Application.Ports;
using Bud.Shared.Contracts;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Application.UnitTests.Application.Goals;

public sealed class GoalObjectiveReadUseCasesTests
{
    private readonly Mock<IGoalRepository> _repository = new();
    private readonly Mock<IGoalProgressService> _progressService = new();

    [Fact]
    public async Task ViewMissionObjectiveDetails_WhenFound_ReturnsObjective()
    {
        var goalId = Guid.NewGuid();
        var objective = new Goal { Id = goalId, OrganizationId = Guid.NewGuid(), Name = "Obj" };

        _repository
            .Setup(repository => repository.GetByIdReadOnlyAsync(goalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(objective);

        var useCase = new GetGoalById(_repository.Object);

        var result = await useCase.ExecuteAsync(goalId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(goalId);
    }

    [Fact]
    public async Task ViewMissionObjectiveDetails_WhenNotFound_ReturnsNotFound()
    {
        _repository
            .Setup(repository => repository.GetByIdReadOnlyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Goal?)null);

        var useCase = new GetGoalById(_repository.Object);

        var result = await useCase.ExecuteAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task ListMissionObjectives_ReturnsPagedResult()
    {
        var parentId = Guid.NewGuid();

        var pagedResult = new PagedResult<Goal>
        {
            Items = [],
            Total = 0,
            Page = 1,
            PageSize = 10
        };

        _repository
            .Setup(repository => repository.ExistsAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repository
            .Setup(repository => repository.GetChildrenAsync(parentId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var useCase = new ListGoalChildren(_repository.Object);

        var result = await useCase.ExecuteAsync(parentId, 1, 10);

        result.IsSuccess.Should().BeTrue();
        _repository.Verify(repository => repository.GetChildrenAsync(parentId, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CalculateMissionObjectiveProgress_DelegatesToProgressService()
    {
        var goalIds = new List<Guid> { Guid.NewGuid() };

        _progressService
            .Setup(service => service.GetProgressAsync(goalIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<GoalProgressSnapshot>>.Success([]));

        var useCase = new ListGoalProgress(_progressService.Object);

        var result = await useCase.ExecuteAsync(goalIds);

        result.IsSuccess.Should().BeTrue();
        _progressService.Verify(service => service.GetProgressAsync(goalIds, It.IsAny<CancellationToken>()), Times.Once);
    }
}
