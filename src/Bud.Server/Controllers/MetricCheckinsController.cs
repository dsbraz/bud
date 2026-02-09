using Bud.Server.Application.MetricCheckins;
using Bud.Server.Authorization;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bud.Server.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.TenantSelected)]
[Route("api/metric-checkins")]
[Produces("application/json")]
public sealed class MetricCheckinsController(
    IMetricCheckinCommandUseCase metricCheckinCommandUseCase,
    IMetricCheckinQueryUseCase metricCheckinQueryUseCase,
    IValidator<CreateMetricCheckinRequest> createValidator,
    IValidator<UpdateMetricCheckinRequest> updateValidator) : ApiControllerBase
{
    /// <summary>
    /// Cria um novo check-in de métrica.
    /// </summary>
    /// <remarks>
    /// Exemplo de payload:
    /// { "missionMetricId": "GUID", "value": 120.5, "text": null, "checkinDate": "2026-02-01", "note": "Evolução semanal", "confidenceLevel": 4 }
    /// </remarks>
    /// <response code="201">Check-in criado com sucesso.</response>
    /// <response code="400">Payload inválido ou erro de validação.</response>
    /// <response code="404">Métrica não encontrada.</response>
    /// <response code="403">Sem permissão para criar check-in.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MetricCheckin), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MetricCheckin>> Create(CreateMetricCheckinRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await metricCheckinCommandUseCase.CreateAsync(User, request, cancellationToken);
        return FromResult(result, checkin => CreatedAtAction(nameof(GetById), new { id = checkin.Id }, checkin));
    }

    /// <summary>
    /// Atualiza um check-in existente.
    /// </summary>
    /// <response code="200">Check-in atualizado com sucesso.</response>
    /// <response code="400">Payload inválido ou erro de validação.</response>
    /// <response code="404">Check-in não encontrado.</response>
    /// <response code="403">Sem permissão para atualizar check-in.</response>
    [HttpPut("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MetricCheckin), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MetricCheckin>> Update(Guid id, UpdateMetricCheckinRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await metricCheckinCommandUseCase.UpdateAsync(User, id, request, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Exclui um check-in por identificador.
    /// </summary>
    /// <response code="204">Check-in removido com sucesso.</response>
    /// <response code="404">Check-in não encontrado.</response>
    /// <response code="403">Sem permissão para excluir check-in.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await metricCheckinCommandUseCase.DeleteAsync(User, id, cancellationToken);
        return FromResult(result, NoContent);
    }

    /// <summary>
    /// Busca um check-in por identificador.
    /// </summary>
    /// <response code="200">Check-in encontrado.</response>
    /// <response code="404">Check-in não encontrado.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MetricCheckin), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MetricCheckin>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await metricCheckinQueryUseCase.GetByIdAsync(id, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Lista check-ins com filtros e paginação.
    /// </summary>
    /// <response code="200">Lista paginada retornada com sucesso.</response>
    /// <response code="400">Parâmetros inválidos.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<MetricCheckin>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<MetricCheckin>>> GetAll(
        [FromQuery] Guid? missionMetricId,
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

        var result = await metricCheckinQueryUseCase.GetAllAsync(missionMetricId, missionId, page, pageSize, cancellationToken);
        return FromResultOk(result);
    }

}
