using Bud.Server.Application.Ports;
using Bud.Server.Domain.Repositories;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Application.UseCases.Goals;
using Bud.Server.Application.ReadModels;

using Bud.Shared.Contracts;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Goals;

public sealed class MissionReadUseCasesTests
{
    private readonly Mock<IGoalRepository> _repo = new();
    private readonly Mock<IGoalProgressService> _progressService = new();

    private GetGoalById CreateGetMissionById()
        => new(_repo.Object);

    private ListGoalProgress CreateListMissionProgress()
        => new(_progressService.Object);

    private ListCollaboratorGoals CreateListCollaboratorMissions()
        => new(_repo.Object);

    private ListGoalIndicators CreateListMissionMetrics()
        => new(_repo.Object);

    [Fact]
    public async Task GetByIdAsync_WithExistingMission_ReturnsSuccess()
    {
        var goalId = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdReadOnlyAsync(goalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Goal { Id = goalId, Name = "M", OrganizationId = Guid.NewGuid() });

        var useCase = CreateGetMissionById();

        var result = await useCase.ExecuteAsync(goalId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(goalId);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetByIdReadOnlyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Goal?)null);

        var useCase = CreateGetMissionById();

        var result = await useCase.ExecuteAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetProgressAsync_DelegatesToProgressService()
    {
        var ids = new List<Guid> { Guid.NewGuid() };
        _progressService
            .Setup(s => s.GetProgressAsync(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<GoalProgressSnapshot>>.Success([]));

        var useCase = CreateListMissionProgress();

        var result = await useCase.ExecuteAsync(ids);

        result.IsSuccess.Should().BeTrue();
        _progressService.Verify(s => s.GetProgressAsync(ids, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMyMissionsAsync_WithNonExistingCollaborator_ReturnsNotFound()
    {
        _repo.Setup(r => r.FindCollaboratorForMyGoalsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collaborator?)null);

        var useCase = CreateListCollaboratorMissions();

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), null, 1, 10);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetMetricsAsync_WhenMissionNotFound_ReturnsNotFound()
    {
        _repo.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = CreateListMissionMetrics();

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), 1, 10);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }
}
