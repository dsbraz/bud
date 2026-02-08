using Bud.Server.Authorization;
using Bud.Server.Application.MissionMetrics;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bud.Server.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.TenantSelected)]
[Route("api/mission-metrics")]
[Produces("application/json")]
public sealed class MissionMetricsController(
    IMissionMetricCommandUseCase missionMetricCommandUseCase,
    IMissionMetricQueryUseCase missionMetricQueryUseCase,
    IValidator<CreateMissionMetricRequest> createValidator,
    IValidator<UpdateMissionMetricRequest> updateValidator) : ApiControllerBase
{
    /// <summary>
    /// Cria uma nova métrica de missão.
    /// </summary>
    /// <remarks>
    /// Exemplo de payload:
    /// { "missionId": "GUID", "name": "Receita recorrente", "type": "Quantitative", "initialValue": 100, "targetValue": 200 }
    /// </remarks>
    /// <response code="201">Métrica criada com sucesso.</response>
    /// <response code="400">Payload inválido ou erro de validação.</response>
    /// <response code="404">Missão não encontrada.</response>
    /// <response code="403">Sem permissão para criar métrica.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MissionMetric), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MissionMetric>> Create(CreateMissionMetricRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await missionMetricCommandUseCase.CreateAsync(User, request, cancellationToken);
        return FromResult(result, metric => CreatedAtAction(nameof(GetById), new { id = metric.Id }, metric));
    }

    /// <summary>
    /// Atualiza uma métrica de missão.
    /// </summary>
    /// <response code="200">Métrica atualizada com sucesso.</response>
    /// <response code="400">Payload inválido ou erro de validação.</response>
    /// <response code="404">Métrica não encontrada.</response>
    /// <response code="403">Sem permissão para atualizar métrica.</response>
    [HttpPut("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MissionMetric), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MissionMetric>> Update(Guid id, UpdateMissionMetricRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await missionMetricCommandUseCase.UpdateAsync(User, id, request, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Exclui uma métrica de missão.
    /// </summary>
    /// <response code="204">Métrica removida com sucesso.</response>
    /// <response code="404">Métrica não encontrada.</response>
    /// <response code="403">Sem permissão para excluir métrica.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await missionMetricCommandUseCase.DeleteAsync(User, id, cancellationToken);
        return FromResult(result, NoContent);
    }

    /// <summary>
    /// Busca uma métrica de missão por identificador.
    /// </summary>
    /// <response code="200">Métrica encontrada.</response>
    /// <response code="404">Métrica não encontrada.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MissionMetric), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MissionMetric>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await missionMetricQueryUseCase.GetByIdAsync(id, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Calcula o progresso das métricas informadas.
    /// </summary>
    /// <response code="200">Progresso calculado com sucesso.</response>
    /// <response code="400">Parâmetro metricIds inválido.</response>
    [HttpGet("progress")]
    [ProducesResponseType(typeof(List<MetricProgressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<MetricProgressDto>>> GetProgress(
        [FromQuery] string ids,
        CancellationToken cancellationToken)
    {
        var parseResult = ParseGuidCsv(ids, "ids");
        if (parseResult.Failure is not null)
        {
            return parseResult.Failure;
        }

        var result = await missionMetricQueryUseCase.GetProgressAsync(parseResult.Values!, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Lista métricas por missão com paginação.
    /// </summary>
    /// <response code="200">Lista paginada retornada com sucesso.</response>
    /// <response code="400">Parâmetros inválidos.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<MissionMetric>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<MissionMetric>>> GetAll(
        [FromQuery] Guid? missionId,
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

        var result = await missionMetricQueryUseCase.GetAllAsync(missionId, searchValidation.Value, page, pageSize, cancellationToken);
        return FromResultOk(result);
    }
}
