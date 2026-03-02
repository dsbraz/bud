using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.UseCases.Tasks;
using Bud.Server.Authorization;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;
using Bud.Shared.Contracts.Requests;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Tasks;

public sealed class TaskWriteUseCasesTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));

    private static Goal MakeGoal() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Meta de Teste",
        StartDate = DateTime.UtcNow,
        EndDate = DateTime.UtcNow.AddDays(30),
        Status = GoalStatus.Active,
        OrganizationId = Guid.NewGuid()
    };

    private static GoalTask MakeTask(Guid? orgId = null, Guid? goalId = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Tarefa de Teste",
        State = TaskState.ToDo,
        OrganizationId = orgId ?? Guid.NewGuid(),
        GoalId = goalId ?? Guid.NewGuid()
    };

    // ── CreateTask ──────────────────────────────────────────────

    [Fact]
    public async Task CreateTask_WhenGoalNotFound_ReturnsNotFound()
    {
        var repo = new Mock<ITaskRepository>(MockBehavior.Strict);
        var auth = new Mock<IApplicationAuthorizationGateway>(MockBehavior.Strict);

        repo.Setup(r => r.GetGoalByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Goal?)null);

        var useCase = new CreateTask(repo.Object, auth.Object, NullLogger<CreateTask>.Instance);

        var result = await useCase.ExecuteAsync(User, new CreateTaskRequest
        {
            GoalId = Guid.NewGuid(),
            Name = "Tarefa",
            State = TaskState.ToDo
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        auth.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateTask_WhenUnauthorized_ReturnsForbidden()
    {
        var goal = MakeGoal();
        var repo = new Mock<ITaskRepository>(MockBehavior.Strict);
        var auth = new Mock<IApplicationAuthorizationGateway>();

        repo.Setup(r => r.GetGoalByIdAsync(goal.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goal);

        auth.Setup(a => a.CanAccessTenantOrganizationAsync(User, goal.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new CreateTask(repo.Object, auth.Object, NullLogger<CreateTask>.Instance);

        var result = await useCase.ExecuteAsync(User, new CreateTaskRequest
        {
            GoalId = goal.Id,
            Name = "Tarefa",
            State = TaskState.ToDo
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task CreateTask_WhenAuthorized_CreatesTaskViaRepository()
    {
        var goal = MakeGoal();
        var repo = new Mock<ITaskRepository>();
        var auth = new Mock<IApplicationAuthorizationGateway>();

        repo.Setup(r => r.GetGoalByIdAsync(goal.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goal);
        repo.Setup(r => r.AddAsync(It.IsAny<GoalTask>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        auth.Setup(a => a.CanAccessTenantOrganizationAsync(User, goal.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new CreateTask(repo.Object, auth.Object, NullLogger<CreateTask>.Instance);

        var result = await useCase.ExecuteAsync(User, new CreateTaskRequest
        {
            GoalId = goal.Id,
            Name = "Implementar feature",
            Description = "Detalhes da feature",
            State = TaskState.Doing
        });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Implementar feature");
        result.Value.GoalId.Should().Be(goal.Id);
        result.Value.OrganizationId.Should().Be(goal.OrganizationId);
        result.Value.State.Should().Be(TaskState.Doing);
        repo.Verify(r => r.AddAsync(It.IsAny<GoalTask>(), It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTask_WithDueDate_ResponseContainsDueDate()
    {
        var goal = MakeGoal();
        var dueDate = new DateTime(2026, 12, 31);
        var repo = new Mock<ITaskRepository>();
        var auth = new Mock<IApplicationAuthorizationGateway>();

        repo.Setup(r => r.GetGoalByIdAsync(goal.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goal);
        repo.Setup(r => r.AddAsync(It.IsAny<GoalTask>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        auth.Setup(a => a.CanAccessTenantOrganizationAsync(User, goal.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new CreateTask(repo.Object, auth.Object, NullLogger<CreateTask>.Instance);

        var result = await useCase.ExecuteAsync(User, new CreateTaskRequest
        {
            GoalId = goal.Id,
            Name = "Tarefa com prazo",
            State = TaskState.ToDo,
            DueDate = dueDate
        });

        result.IsSuccess.Should().BeTrue();
        result.Value!.DueDate.Should().Be(dueDate);
    }

    // ── PatchTask ───────────────────────────────────────────────

    [Fact]
    public async Task PatchTask_WhenNotFound_ReturnsNotFound()
    {
        var repo = new Mock<ITaskRepository>(MockBehavior.Strict);
        var auth = new Mock<IApplicationAuthorizationGateway>(MockBehavior.Strict);

        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoalTask?)null);

        var useCase = new PatchTask(repo.Object, auth.Object, NullLogger<PatchTask>.Instance);

        var result = await useCase.ExecuteAsync(User, Guid.NewGuid(), new PatchTaskRequest());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        auth.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PatchTask_WhenUnauthorized_ReturnsForbidden()
    {
        var task = MakeTask();
        var repo = new Mock<ITaskRepository>(MockBehavior.Strict);
        var auth = new Mock<IApplicationAuthorizationGateway>();

        repo.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        auth.Setup(a => a.CanAccessTenantOrganizationAsync(User, task.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new PatchTask(repo.Object, auth.Object, NullLogger<PatchTask>.Instance);

        var result = await useCase.ExecuteAsync(User, task.Id, new PatchTaskRequest());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task PatchTask_WhenAuthorized_UpdatesAndReturnsSuccess()
    {
        var task = MakeTask();
        var repo = new Mock<ITaskRepository>();
        var auth = new Mock<IApplicationAuthorizationGateway>();

        repo.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        auth.Setup(a => a.CanAccessTenantOrganizationAsync(User, task.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new PatchTask(repo.Object, auth.Object, NullLogger<PatchTask>.Instance);

        var request = new PatchTaskRequest
        {
            State = TaskState.Done
        };

        var result = await useCase.ExecuteAsync(User, task.Id, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.State.Should().Be(TaskState.Done);
    }

    [Fact]
    public async Task PatchTask_WithDueDate_ResponseContainsDueDate()
    {
        var task = MakeTask();
        var dueDate = new DateTime(2026, 9, 30);
        var repo = new Mock<ITaskRepository>();
        var auth = new Mock<IApplicationAuthorizationGateway>();

        repo.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        auth.Setup(a => a.CanAccessTenantOrganizationAsync(User, task.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new PatchTask(repo.Object, auth.Object, NullLogger<PatchTask>.Instance);

        var request = new PatchTaskRequest
        {
            DueDate = dueDate
        };

        var result = await useCase.ExecuteAsync(User, task.Id, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DueDate.Should().Be(dueDate);
    }

    // ── DeleteTask ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteTask_WhenNotFound_ReturnsNotFound()
    {
        var repo = new Mock<ITaskRepository>(MockBehavior.Strict);
        var auth = new Mock<IApplicationAuthorizationGateway>(MockBehavior.Strict);

        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoalTask?)null);

        var useCase = new DeleteTask(repo.Object, auth.Object, NullLogger<DeleteTask>.Instance);

        var result = await useCase.ExecuteAsync(User, Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        auth.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteTask_WhenUnauthorized_ReturnsForbidden()
    {
        var task = MakeTask();
        var repo = new Mock<ITaskRepository>(MockBehavior.Strict);
        var auth = new Mock<IApplicationAuthorizationGateway>();

        repo.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        auth.Setup(a => a.CanAccessTenantOrganizationAsync(User, task.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new DeleteTask(repo.Object, auth.Object, NullLogger<DeleteTask>.Instance);

        var result = await useCase.ExecuteAsync(User, task.Id);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task DeleteTask_WhenAuthorized_RemovesAndReturnsSuccess()
    {
        var task = MakeTask();
        var repo = new Mock<ITaskRepository>();
        var auth = new Mock<IApplicationAuthorizationGateway>();

        repo.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        repo.Setup(r => r.RemoveAsync(task, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        auth.Setup(a => a.CanAccessTenantOrganizationAsync(User, task.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new DeleteTask(repo.Object, auth.Object, NullLogger<DeleteTask>.Instance);

        var result = await useCase.ExecuteAsync(User, task.Id);

        result.IsSuccess.Should().BeTrue();
        repo.Verify(r => r.RemoveAsync(task, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
