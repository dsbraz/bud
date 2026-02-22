using Bud.Server.Application.Objectives;
using Bud.Server.Authorization;
using Bud.Shared.Contracts;
using Bud.Server.Domain.Model;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bud.Server.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.TenantSelected)]
[Route("api/objectives")]
[Produces("application/json")]
public sealed class ObjectivesController(
    CreateObjective createObjective,
    PatchObjective patchObjective,
    DeleteObjective deleteObjective,
    GetObjectiveById getObjectiveById,
    ListObjectives listObjectives,
    ListObjectiveMetrics listObjectiveMetrics,
    ListObjectiveProgress listObjectiveProgress,
    IValidator<CreateObjectiveRequest> createValidator,
    IValidator<PatchObjectiveRequest> updateValidator) : ApiControllerBase
{
    /// <summary>
    /// Cria um novo objetivo de missão.
    /// </summary>
    /// <remarks>
    /// Exemplo de payload:
    /// { "missionId": "GUID", "name": "Objetivo estratégico", "description": "Descrição opcional", "dimension": "Clientes" }
    /// </remarks>
    /// <response code="201">Objetivo criado com sucesso.</response>
    /// <response code="400">Payload inválido ou erro de validação.</response>
    /// <response code="404">Missão não encontrada.</response>
    /// <response code="403">Sem permissão para criar objetivo.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(Objective), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Objective>> Create(CreateObjectiveRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await createObjective.ExecuteAsync(User, request, cancellationToken);
        return FromResult(result, objective => CreatedAtAction(nameof(GetById), new { id = objective.Id }, objective));
    }

    /// <summary>
    /// Atualiza um objetivo de missão.
    /// </summary>
    /// <response code="200">Objetivo atualizado com sucesso.</response>
    /// <response code="400">Payload inválido ou erro de validação.</response>
    /// <response code="404">Objetivo não encontrado.</response>
    /// <response code="403">Sem permissão para atualizar objetivo.</response>
    [HttpPatch("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(Objective), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Objective>> Update(Guid id, PatchObjectiveRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await patchObjective.ExecuteAsync(User, id, request, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Exclui um objetivo de missão.
    /// </summary>
    /// <response code="204">Objetivo removido com sucesso.</response>
    /// <response code="404">Objetivo não encontrado.</response>
    /// <response code="403">Sem permissão para excluir objetivo.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await deleteObjective.ExecuteAsync(User, id, cancellationToken);
        return FromResult(result, NoContent);
    }

    /// <summary>
    /// Busca um objetivo de missão por identificador.
    /// </summary>
    /// <response code="200">Objetivo encontrado.</response>
    /// <response code="404">Objetivo não encontrado.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Objective), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Objective>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await getObjectiveById.ExecuteAsync(id, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Lista objetivos de uma missão com paginação.
    /// </summary>
    /// <response code="200">Lista paginada retornada com sucesso.</response>
    /// <response code="400">Parâmetros inválidos.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Objective>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<Objective>>> GetAll(
        [FromQuery] Guid? missionId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var paginationValidation = ValidatePagination(page, pageSize);
        if (paginationValidation is not null)
        {
            return paginationValidation;
        }

        var result = await listObjectives.ExecuteAsync(missionId, page, pageSize, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Lista métricas de um objetivo com paginação.
    /// </summary>
    /// <response code="200">Lista paginada retornada com sucesso.</response>
    /// <response code="400">Parâmetros inválidos.</response>
    [HttpGet("{objectiveId:guid}/metrics")]
    [ProducesResponseType(typeof(PagedResult<Metric>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<Metric>>> GetMetrics(
        Guid objectiveId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var paginationValidation = ValidatePagination(page, pageSize);
        if (paginationValidation is not null)
        {
            return paginationValidation;
        }

        var result = await listObjectiveMetrics.ExecuteAsync(objectiveId, page, pageSize, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Calcula o progresso dos objetivos informados.
    /// </summary>
    /// <response code="200">Progresso calculado com sucesso.</response>
    /// <response code="400">Parâmetro ids inválido.</response>
    [HttpGet("progress")]
    [ProducesResponseType(typeof(List<ObjectiveProgressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<ObjectiveProgressDto>>> GetProgress(
        [FromQuery] string ids,
        CancellationToken cancellationToken)
    {
        var parseResult = ParseGuidCsv(ids, "ids");
        if (parseResult.Failure is not null)
        {
            return parseResult.Failure;
        }

        var result = await listObjectiveProgress.ExecuteAsync(parseResult.Values!, cancellationToken);
        return FromResultOk(result);
    }
}
