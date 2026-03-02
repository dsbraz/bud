using Bud.Server.Application.UseCases.Goals;
using Bud.Server.Application.UseCases.Tasks;
using Bud.Server.Authorization;
using Bud.Shared.Contracts;
using Bud.Shared.Contracts.Requests;
using Bud.Shared.Contracts.Responses;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bud.Server.Domain.Model;

namespace Bud.Server.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.TenantSelected)]
[Route("api/goals")]
[Produces("application/json")]
public sealed class GoalsController(
    CreateGoal createGoal,
    PatchGoal patchGoal,
    DeleteGoal deleteGoal,
    GetGoalById getGoalById,
    ListGoals listGoals,
    ListGoalProgress listGoalProgress,
    ListGoalIndicators listGoalIndicators,
    ListGoalChildren listGoalChildren,
    ListTasks listTasks,
    IValidator<CreateGoalRequest> createValidator,
    IValidator<PatchGoalRequest> updateValidator) : ApiControllerBase
{
    /// <summary>
    /// Cria uma nova meta.
    /// </summary>
    /// <remarks>
    /// Exemplo de payload:
    /// { "name": "Aumentar NPS", "scopeType": "Workspace", "scopeId": "GUID", "startDate": "2026-01-01", "endDate": "2026-03-31" }
    /// </remarks>
    /// <response code="201">Meta criada com sucesso.</response>
    /// <response code="400">Payload inválido ou erro de validação.</response>
    /// <response code="404">Escopo da meta não encontrado.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(GoalResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Goal>> Create(CreateGoalRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await createGoal.ExecuteAsync(User, request, cancellationToken);
        return FromResult(result, goal => CreatedAtAction(nameof(GetById), new { id = goal.Id }, goal));
    }

    /// <summary>
    /// Atualiza uma meta existente.
    /// </summary>
    /// <remarks>
    /// Exemplo de payload:
    /// { "name": "Aumentar NPS em 10 pontos", "status": "InProgress", "endDate": "2026-04-30" }
    /// </remarks>
    /// <response code="200">Meta atualizada com sucesso.</response>
    /// <response code="400">Payload inválido ou erro de validação.</response>
    /// <response code="404">Meta não encontrada.</response>
    [HttpPatch("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(GoalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Goal>> Update(Guid id, PatchGoalRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await patchGoal.ExecuteAsync(User, id, request, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Remove uma meta pelo identificador.
    /// </summary>
    /// <response code="204">Meta removida com sucesso.</response>
    /// <response code="404">Meta não encontrada.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await deleteGoal.ExecuteAsync(User, id, cancellationToken);
        return FromResult(result, NoContent);
    }

    /// <summary>
    /// Busca uma meta pelo identificador.
    /// </summary>
    /// <response code="200">Meta encontrada.</response>
    /// <response code="404">Meta não encontrada.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GoalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Goal>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await getGoalById.ExecuteAsync(id, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Lista metas com filtros e paginação.
    /// </summary>
    /// <response code="200">Lista paginada retornada com sucesso.</response>
    /// <response code="400">Parâmetros de filtro/paginação inválidos.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Goal>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<Goal>>> GetAll(
        [FromQuery] GoalScopeType? scopeType,
        [FromQuery] Guid? scopeId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var searchValidation = ValidateAndNormalizeSearch(search);
        if (searchValidation.Failure is not null)
        {
            return searchValidation.Failure;
        }

        var paginationValidation = ValidatePagination(page, pageSize);
        if (paginationValidation is not null)
        {
            return paginationValidation;
        }

        var result = await listGoals.ExecuteAsync(scopeType, scopeId, searchValidation.Value, page, pageSize, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Retorna progresso agregado de metas.
    /// </summary>
    /// <response code="200">Progresso calculado com sucesso.</response>
    /// <response code="400">Parâmetro ids inválido.</response>
    [HttpGet("progress")]
    [ProducesResponseType(typeof(List<GoalProgressResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<GoalProgressResponse>>> GetProgress(
        [FromQuery] string ids,
        CancellationToken cancellationToken)
    {
        var parseResult = ParseGuidCsv(ids, "ids");
        if (parseResult.Failure is not null)
        {
            return parseResult.Failure;
        }

        var result = await listGoalProgress.ExecuteAsync(parseResult.Values!, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Lista indicadores associados a uma meta.
    /// </summary>
    /// <response code="200">Indicadores retornados com sucesso.</response>
    /// <response code="400">Parâmetros de paginação inválidos.</response>
    /// <response code="404">Meta não encontrada.</response>
    [HttpGet("{id:guid}/indicators")]
    [ProducesResponseType(typeof(PagedResult<Indicator>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<Indicator>>> GetIndicators(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var paginationValidation = ValidatePagination(page, pageSize);
        if (paginationValidation is not null)
        {
            return paginationValidation;
        }

        var result = await listGoalIndicators.ExecuteAsync(id, page, pageSize, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Lista submetas de uma meta com paginação.
    /// </summary>
    /// <response code="200">Submetas retornadas com sucesso.</response>
    /// <response code="400">Parâmetros de paginação inválidos.</response>
    /// <response code="404">Meta não encontrada.</response>
    [HttpGet("{id:guid}/children")]
    [ProducesResponseType(typeof(PagedResult<Goal>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<Goal>>> GetChildren(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var paginationValidation = ValidatePagination(page, pageSize);
        if (paginationValidation is not null)
        {
            return paginationValidation;
        }

        var result = await listGoalChildren.ExecuteAsync(id, page, pageSize, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Lista tarefas de uma meta.
    /// </summary>
    /// <response code="200">Tarefas retornadas com sucesso.</response>
    /// <response code="400">Parâmetros de paginação inválidos.</response>
    /// <response code="404">Meta não encontrada.</response>
    [HttpGet("{id:guid}/tasks")]
    [ProducesResponseType(typeof(PagedResult<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<TaskResponse>>> GetTasks(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var paginationValidation = ValidatePagination(page, pageSize);
        if (paginationValidation is not null)
        {
            return paginationValidation;
        }

        var result = await listTasks.ExecuteAsync(id, page, pageSize, cancellationToken);
        return FromResultOk(result);
    }
}
