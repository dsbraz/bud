using Bud.Server.Application.MissionObjectives;
using Bud.Server.Application.Missions;
using Bud.Server.Authorization;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bud.Server.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.TenantSelected)]
[Route("api/missions")]
[Produces("application/json")]
public sealed class MissionsController(
    IMissionCommandUseCase missionCommandUseCase,
    IMissionQueryUseCase missionQueryUseCase,
    IMissionObjectiveQueryUseCase missionObjectiveQueryUseCase,
    IValidator<CreateMissionRequest> createValidator,
    IValidator<UpdateMissionRequest> updateValidator) : ApiControllerBase
{
    /// <summary>
    /// Cria uma nova missão.
    /// </summary>
    /// <remarks>
    /// Exemplo de payload:
    /// { "name": "Aumentar NPS", "scopeType": "Workspace", "scopeId": "GUID", "startDate": "2026-01-01", "endDate": "2026-03-31" }
    /// </remarks>
    /// <response code="201">Missão criada com sucesso.</response>
    /// <response code="400">Payload inválido ou erro de validação.</response>
    /// <response code="404">Escopo da missão não encontrado.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(Mission), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Mission>> Create(CreateMissionRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await missionCommandUseCase.CreateAsync(User, request, cancellationToken);
        return FromResult(result, mission => CreatedAtAction(nameof(GetById), new { id = mission.Id }, mission));
    }

    /// <summary>
    /// Atualiza uma missão existente.
    /// </summary>
    /// <remarks>
    /// Exemplo de payload:
    /// { "name": "Aumentar NPS em 10 pontos", "status": "InProgress", "endDate": "2026-04-30" }
    /// </remarks>
    /// <response code="200">Missão atualizada com sucesso.</response>
    /// <response code="400">Payload inválido ou erro de validação.</response>
    /// <response code="404">Missão não encontrada.</response>
    [HttpPut("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(Mission), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Mission>> Update(Guid id, UpdateMissionRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await missionCommandUseCase.UpdateAsync(User, id, request, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Remove uma missão pelo identificador.
    /// </summary>
    /// <response code="204">Missão removida com sucesso.</response>
    /// <response code="404">Missão não encontrada.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await missionCommandUseCase.DeleteAsync(User, id, cancellationToken);
        return FromResult(result, NoContent);
    }

    /// <summary>
    /// Busca uma missão pelo identificador.
    /// </summary>
    /// <response code="200">Missão encontrada.</response>
    /// <response code="404">Missão não encontrada.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Mission), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Mission>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await missionQueryUseCase.GetByIdAsync(id, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Lista missões com filtros e paginação.
    /// </summary>
    /// <response code="200">Lista paginada retornada com sucesso.</response>
    /// <response code="400">Parâmetros de filtro/paginação inválidos.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Mission>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<Mission>>> GetAll(
        [FromQuery] MissionScopeType? scopeType,
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

        var result = await missionQueryUseCase.GetAllAsync(scopeType, scopeId, searchValidation.Value, page, pageSize, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Lista missões atribuídas ao colaborador informado.
    /// </summary>
    /// <response code="200">Lista paginada retornada com sucesso.</response>
    /// <response code="400">Parâmetros inválidos.</response>
    /// <response code="404">Colaborador não encontrado.</response>
    [HttpGet("my-missions/{collaboratorId:guid}")]
    [ProducesResponseType(typeof(PagedResult<Mission>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<Mission>>> GetMyMissions(
        Guid collaboratorId,
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

        var result = await missionQueryUseCase.GetMyMissionsAsync(collaboratorId, searchValidation.Value, page, pageSize, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Retorna progresso agregado de missões.
    /// </summary>
    /// <response code="200">Progresso calculado com sucesso.</response>
    /// <response code="400">Parâmetro missionIds inválido.</response>
    [HttpGet("progress")]
    [ProducesResponseType(typeof(List<MissionProgressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<MissionProgressDto>>> GetProgress(
        [FromQuery] string ids,
        CancellationToken cancellationToken)
    {
        var parseResult = ParseGuidCsv(ids, "ids");
        if (parseResult.Failure is not null)
        {
            return parseResult.Failure;
        }

        var result = await missionQueryUseCase.GetProgressAsync(parseResult.Values!, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Lista métricas associadas a uma missão.
    /// </summary>
    /// <response code="200">Métricas retornadas com sucesso.</response>
    /// <response code="400">Parâmetros de paginação inválidos.</response>
    /// <response code="404">Missão não encontrada.</response>
    [HttpGet("{id:guid}/metrics")]
    [ProducesResponseType(typeof(PagedResult<MissionMetric>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<MissionMetric>>> GetMetrics(
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

        var result = await missionQueryUseCase.GetMetricsAsync(id, page, pageSize, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Lista objetivos associados a uma missão.
    /// </summary>
    /// <response code="200">Objetivos retornados com sucesso.</response>
    /// <response code="400">Parâmetros de paginação inválidos.</response>
    [HttpGet("{id:guid}/objectives")]
    [ProducesResponseType(typeof(PagedResult<MissionObjective>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<MissionObjective>>> GetObjectives(
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

        var result = await missionObjectiveQueryUseCase.GetByMissionAsync(id, page, pageSize, cancellationToken);
        return FromResultOk(result);
    }
}
