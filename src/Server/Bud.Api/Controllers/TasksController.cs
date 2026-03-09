using Bud.Application.UseCases.Tasks;
using Bud.Api.Authorization;
using Bud.Shared.Contracts.Requests;
using Bud.Shared.Contracts.Responses;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bud.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.TenantSelected)]
[Route("api")]
[Produces("application/json")]
public sealed class TasksController(
    CreateTask createTask,
    PatchTask patchTask,
    DeleteTask deleteTask,
    IValidator<CreateTaskRequest> createValidator,
    IValidator<PatchTaskRequest> updateValidator) : ApiControllerBase
{
    /// <summary>
    /// Cria uma nova tarefa para uma meta.
    /// </summary>
    /// <response code="201">Tarefa criada com sucesso.</response>
    /// <response code="400">Payload inválido ou erro de validação.</response>
    /// <response code="404">Meta não encontrada.</response>
    /// <response code="403">Sem permissão para criar tarefa.</response>
    [HttpPost("goals/{goalId:guid}/tasks")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TaskResponse>> Create(Guid goalId, CreateTaskRequest request, CancellationToken cancellationToken)
    {
        request.GoalId = goalId;

        var validationResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await createTask.ExecuteAsync(User, request, cancellationToken);
        return FromResult(result, task => CreatedAtAction(nameof(Update), new { id = task.Id }, task));
    }

    /// <summary>
    /// Atualiza uma tarefa existente.
    /// </summary>
    /// <response code="200">Tarefa atualizada com sucesso.</response>
    /// <response code="400">Payload inválido ou erro de validação.</response>
    /// <response code="404">Tarefa não encontrada.</response>
    /// <response code="403">Sem permissão para atualizar tarefa.</response>
    [HttpPatch("tasks/{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TaskResponse>> Update(Guid id, PatchTaskRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await patchTask.ExecuteAsync(User, id, request, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Remove uma tarefa.
    /// </summary>
    /// <response code="204">Tarefa removida com sucesso.</response>
    /// <response code="404">Tarefa não encontrada.</response>
    /// <response code="403">Sem permissão para excluir tarefa.</response>
    [HttpDelete("tasks/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await deleteTask.ExecuteAsync(User, id, cancellationToken);
        return FromResult(result, NoContent);
    }
}
