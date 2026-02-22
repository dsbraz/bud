using Bud.Server.Application.Metrics;
using Bud.Server.Authorization;
using Bud.Shared.Contracts;
using Bud.Server.Domain.Model;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bud.Server.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.TenantSelected)]
[Route("api/metrics")]
[Produces("application/json")]
public sealed class MetricsController(
    CreateMetric createMetric,
    PatchMetric patchMetric,
    DeleteMetric deleteMetric,
    GetMetricById getMetricById,
    ListMetrics listMetrics,
    ListMetricProgress listMetricProgress,
    CreateMetricCheckin createMetricCheckin,
    PatchMetricCheckin patchMetricCheckin,
    DeleteMetricCheckin deleteMetricCheckin,
    GetMetricCheckinById getMetricCheckinById,
    ListMetricCheckins listMetricCheckins,
    IValidator<CreateMetricRequest> createValidator,
    IValidator<PatchMetricRequest> updateValidator,
    IValidator<CreateCheckinRequest> createCheckinValidator,
    IValidator<PatchCheckinRequest> patchCheckinValidator) : ApiControllerBase
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
    [ProducesResponseType(typeof(Metric), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Metric>> Create(CreateMetricRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await createMetric.ExecuteAsync(User, request, cancellationToken);
        return FromResult(result, metric => CreatedAtAction(nameof(GetById), new { id = metric.Id }, metric));
    }

    /// <summary>
    /// Atualiza uma métrica de missão.
    /// </summary>
    /// <response code="200">Métrica atualizada com sucesso.</response>
    /// <response code="400">Payload inválido ou erro de validação.</response>
    /// <response code="404">Métrica não encontrada.</response>
    /// <response code="403">Sem permissão para atualizar métrica.</response>
    [HttpPatch("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(Metric), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Metric>> Update(Guid id, PatchMetricRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await patchMetric.ExecuteAsync(User, id, request, cancellationToken);
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
        var result = await deleteMetric.ExecuteAsync(User, id, cancellationToken);
        return FromResult(result, NoContent);
    }

    /// <summary>
    /// Busca uma métrica de missão por identificador.
    /// </summary>
    /// <response code="200">Métrica encontrada.</response>
    /// <response code="404">Métrica não encontrada.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Metric), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Metric>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await getMetricById.ExecuteAsync(id, cancellationToken);
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

        var result = await listMetricProgress.ExecuteAsync(parseResult.Values!, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Lista métricas por missão com paginação.
    /// </summary>
    /// <response code="200">Lista paginada retornada com sucesso.</response>
    /// <response code="400">Parâmetros inválidos.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Metric>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<Metric>>> GetAll(
        [FromQuery] Guid? missionId,
        [FromQuery] Guid? objectiveId,
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

        var result = await listMetrics.ExecuteAsync(
            missionId,
            objectiveId,
            searchValidation.Value,
            page,
            pageSize,
            cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Cria um check-in para uma métrica.
    /// </summary>
    [HttpPost("{metricId:guid}/checkins")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MetricCheckin), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MetricCheckin>> CreateCheckin(
        Guid metricId,
        CreateCheckinRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await createCheckinValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await createMetricCheckin.ExecuteAsync(User, metricId, request, cancellationToken);
        return FromResult(result, checkin =>
            CreatedAtAction(nameof(GetCheckinById), new { metricId, checkinId = checkin.Id }, checkin));
    }

    /// <summary>
    /// Lista check-ins de uma métrica com paginação.
    /// </summary>
    [HttpGet("{metricId:guid}/checkins")]
    [ProducesResponseType(typeof(PagedResult<MetricCheckin>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<MetricCheckin>>> GetCheckins(
        Guid metricId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var paginationValidation = ValidatePagination(page, pageSize);
        if (paginationValidation is not null)
        {
            return paginationValidation;
        }

        var result = await listMetricCheckins.ExecuteAsync(metricId, page, pageSize, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Busca um check-in por identificador dentro da métrica.
    /// </summary>
    [HttpGet("{metricId:guid}/checkins/{checkinId:guid}")]
    [ProducesResponseType(typeof(MetricCheckin), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MetricCheckin>> GetCheckinById(
        Guid metricId,
        Guid checkinId,
        CancellationToken cancellationToken)
    {
        var result = await getMetricCheckinById.ExecuteAsync(metricId, checkinId, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Atualiza um check-in dentro da métrica.
    /// </summary>
    [HttpPatch("{metricId:guid}/checkins/{checkinId:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MetricCheckin), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MetricCheckin>> PatchCheckin(
        Guid metricId,
        Guid checkinId,
        PatchCheckinRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await patchCheckinValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await patchMetricCheckin.ExecuteAsync(User, metricId, checkinId, request, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Exclui um check-in dentro da métrica.
    /// </summary>
    [HttpDelete("{metricId:guid}/checkins/{checkinId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteCheckin(
        Guid metricId,
        Guid checkinId,
        CancellationToken cancellationToken)
    {
        var result = await deleteMetricCheckin.ExecuteAsync(User, metricId, checkinId, cancellationToken);
        return FromResult(result, NoContent);
    }
}
