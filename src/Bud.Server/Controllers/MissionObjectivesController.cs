using Bud.Server.Application.MissionObjectives;
using Bud.Server.Authorization;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bud.Server.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.TenantSelected)]
[Route("api/mission-objectives")]
[Produces("application/json")]
public sealed class MissionObjectivesController(
    IMissionObjectiveCommandUseCase missionObjectiveCommandUseCase,
    IMissionObjectiveQueryUseCase missionObjectiveQueryUseCase,
    IValidator<CreateMissionObjectiveRequest> createValidator,
    IValidator<UpdateMissionObjectiveRequest> updateValidator) : ApiControllerBase
{
    /// <summary>
    /// Cria um novo objetivo de missão.
    /// </summary>
    /// <remarks>
    /// Exemplo de payload:
    /// { "missionId": "GUID", "name": "Objetivo estratégico", "description": "Descrição opcional", "parentObjectiveId": null }
    /// </remarks>
    /// <response code="201">Objetivo criado com sucesso.</response>
    /// <response code="400">Payload inválido ou erro de validação.</response>
    /// <response code="404">Missão ou objetivo pai não encontrado.</response>
    /// <response code="403">Sem permissão para criar objetivo.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MissionObjective), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MissionObjective>> Create(CreateMissionObjectiveRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await missionObjectiveCommandUseCase.CreateAsync(User, request, cancellationToken);
        return FromResult(result, objective => CreatedAtAction(nameof(GetById), new { id = objective.Id }, objective));
    }

    /// <summary>
    /// Atualiza um objetivo de missão.
    /// </summary>
    /// <response code="200">Objetivo atualizado com sucesso.</response>
    /// <response code="400">Payload inválido ou erro de validação.</response>
    /// <response code="404">Objetivo não encontrado.</response>
    /// <response code="403">Sem permissão para atualizar objetivo.</response>
    [HttpPut("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MissionObjective), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MissionObjective>> Update(Guid id, UpdateMissionObjectiveRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await missionObjectiveCommandUseCase.UpdateAsync(User, id, request, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Exclui um objetivo de missão.
    /// </summary>
    /// <response code="204">Objetivo removido com sucesso.</response>
    /// <response code="404">Objetivo não encontrado.</response>
    /// <response code="400">Objetivo possui sub-objetivos e não pode ser excluído.</response>
    /// <response code="403">Sem permissão para excluir objetivo.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await missionObjectiveCommandUseCase.DeleteAsync(User, id, cancellationToken);
        return FromResult(result, NoContent);
    }

    /// <summary>
    /// Busca um objetivo de missão por identificador.
    /// </summary>
    /// <response code="200">Objetivo encontrado.</response>
    /// <response code="404">Objetivo não encontrado.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MissionObjective), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MissionObjective>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await missionObjectiveQueryUseCase.GetByIdAsync(id, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Lista objetivos de uma missão com paginação.
    /// </summary>
    /// <remarks>
    /// Quando parentObjectiveId não é informado, retorna apenas objetivos de nível superior (sem pai).
    /// Quando informado, retorna os sub-objetivos do objetivo pai especificado.
    /// </remarks>
    /// <response code="200">Lista paginada retornada com sucesso.</response>
    /// <response code="400">Parâmetros inválidos.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<MissionObjective>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<MissionObjective>>> GetAll(
        [FromQuery] Guid missionId,
        [FromQuery] Guid? parentObjectiveId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var paginationValidation = ValidatePagination(page, pageSize);
        if (paginationValidation is not null)
        {
            return paginationValidation;
        }

        var result = await missionObjectiveQueryUseCase.GetByMissionAsync(missionId, parentObjectiveId, page, pageSize, cancellationToken);
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

        var result = await missionObjectiveQueryUseCase.GetProgressAsync(parseResult.Values!, cancellationToken);
        return FromResultOk(result);
    }
}
