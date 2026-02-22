using Bud.Server.Application.Ports;
using Bud.Server.Domain.Repositories;
using Bud.Server.Application.Common;
using Bud.Server.Application.Missions;
using Bud.Server.Domain.ReadModels;

using Bud.Shared.Contracts;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Missions;

public sealed class MissionReadUseCasesTests
{
    private readonly Mock<IMissionRepository> _repo = new();
    private readonly Mock<IMissionProgressService> _progressService = new();

    private GetMissionById CreateGetMissionById()
        => new(_repo.Object);

    private ListMissionProgress CreateListMissionProgress()
        => new(_progressService.Object);

    private ListCollaboratorMissions CreateListCollaboratorMissions()
        => new(_repo.Object);

    private ListMissionMetrics CreateListMissionMetrics()
        => new(_repo.Object);

    [Fact]
    public async Task GetByIdAsync_WithExistingMission_ReturnsSuccess()
    {
        var missionId = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdReadOnlyAsync(missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Mission { Id = missionId, Name = "M", OrganizationId = Guid.NewGuid() });

        var useCase = CreateGetMissionById();

        var result = await useCase.ExecuteAsync(missionId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(missionId);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetByIdReadOnlyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mission?)null);

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
            .ReturnsAsync(Result<List<MissionProgressSnapshot>>.Success([]));

        var useCase = CreateListMissionProgress();

        var result = await useCase.ExecuteAsync(ids);

        result.IsSuccess.Should().BeTrue();
        _progressService.Verify(s => s.GetProgressAsync(ids, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMyMissionsAsync_WithNonExistingCollaborator_ReturnsNotFound()
    {
        _repo.Setup(r => r.FindCollaboratorForMyMissionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
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
