using Bud.Server.Validators;
using Bud.Shared.Contracts;
using Bud.Shared.Contracts.Requests;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Validators;

public sealed class TaskValidatorTests
{
    private readonly CreateTaskValidator _createValidator = new();
    private readonly PatchTaskValidator _patchValidator = new();

    // ── CreateTaskValidator ──────────────────────────────────────

    [Fact]
    public async Task Create_WithValidRequest_Passes()
    {
        var request = new CreateTaskRequest
        {
            GoalId = Guid.NewGuid(),
            Name = "Implementar feature",
            State = TaskState.ToDo
        };

        var result = await _createValidator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Create_WithEmptyGoalId_Fails()
    {
        var request = new CreateTaskRequest
        {
            GoalId = Guid.Empty,
            Name = "Tarefa",
            State = TaskState.ToDo
        };

        var result = await _createValidator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTaskRequest.GoalId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_WithEmptyName_Fails(string name)
    {
        var request = new CreateTaskRequest
        {
            GoalId = Guid.NewGuid(),
            Name = name,
            State = TaskState.ToDo
        };

        var result = await _createValidator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTaskRequest.Name));
    }

    [Fact]
    public async Task Create_WithNameExceeding200Chars_Fails()
    {
        var request = new CreateTaskRequest
        {
            GoalId = Guid.NewGuid(),
            Name = new string('A', 201),
            State = TaskState.ToDo
        };

        var result = await _createValidator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTaskRequest.Name));
    }

    [Fact]
    public async Task Create_WithDescriptionExceeding2000Chars_Fails()
    {
        var request = new CreateTaskRequest
        {
            GoalId = Guid.NewGuid(),
            Name = "Tarefa",
            Description = new string('A', 2001),
            State = TaskState.ToDo
        };

        var result = await _createValidator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTaskRequest.Description));
    }

    // ── PatchTaskValidator ───────────────────────────────────────

    [Fact]
    public async Task Patch_WithNoFields_Passes()
    {
        var result = await _patchValidator.ValidateAsync(new PatchTaskRequest());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Patch_WithValidFields_Passes()
    {
        var request = new PatchTaskRequest
        {
            Name = "Nome atualizado",
            State = TaskState.Done
        };

        var result = await _patchValidator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Patch_WithEmptyName_Fails(string name)
    {
        var request = new PatchTaskRequest
        {
            Name = name
        };

        var result = await _patchValidator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Name"));
    }

    [Fact]
    public async Task Patch_WithDescriptionExceeding2000Chars_Fails()
    {
        var request = new PatchTaskRequest
        {
            Description = new string('A', 2001)
        };

        var result = await _patchValidator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Description"));
    }
}
