using Bud.Api.Authorization;
using Bud.Shared.Contracts;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bud.Api.Indicators;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.TenantSelected)]
[Route("api/indicators")]
[Produces("application/json")]
public sealed class IndicatorsController(
    CreateIndicator createIndicator,
    PatchIndicator patchIndicator,
    DeleteIndicator deleteIndicator,
    GetIndicatorById getIndicatorById,
    ListIndicators listIndicators,
    GetIndicatorProgress getIndicatorProgress,
    CreateCheckin createCheckin,
    PatchCheckin patchCheckin,
    DeleteCheckin deleteCheckin,
    GetCheckinById getCheckinById,
    ListCheckins listCheckins,
    IValidator<CreateIndicatorRequest> createValidator,
    IValidator<PatchIndicatorRequest> updateValidator,
    IValidator<CreateCheckinRequest> createCheckinValidator,
    IValidator<PatchCheckinRequest> patchCheckinValidator) : ApiControllerBase
{
    /// <summary>
    /// Cria um novo indicador de meta.
    /// </summary>
    /// <remarks>
    /// Exemplo de payload:
    /// { "goalId": "GUID", "name": "Receita recorrente", "type": "Quantitative" }
    /// </remarks>
    /// <response code="201">Indicador criado com sucesso.</response>
    /// <response code="400">Payload inválido ou erro de validação.</response>
    /// <response code="404">Meta não encontrada.</response>
    /// <response code="403">Sem permissão para criar indicador.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(IndicatorResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Indicator>> Create(CreateIndicatorRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await createIndicator.ExecuteAsync(User, request, cancellationToken);
        return FromResult(result, indicator => CreatedAtAction(nameof(GetById), new { id = indicator.Id }, indicator));
    }

    /// <summary>
    /// Atualiza um indicador de meta.
    /// </summary>
    /// <response code="200">Indicador atualizado com sucesso.</response>
    /// <response code="400">Payload inválido ou erro de validação.</response>
    /// <response code="404">Indicador não encontrado.</response>
    /// <response code="403">Sem permissão para atualizar indicador.</response>
    [HttpPatch("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(IndicatorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Indicator>> Update(Guid id, PatchIndicatorRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await patchIndicator.ExecuteAsync(User, id, request, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Exclui um indicador de meta.
    /// </summary>
    /// <response code="204">Indicador removido com sucesso.</response>
    /// <response code="404">Indicador não encontrado.</response>
    /// <response code="403">Sem permissão para excluir indicador.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await deleteIndicator.ExecuteAsync(User, id, cancellationToken);
        return FromResult(result, NoContent);
    }

    /// <summary>
    /// Busca um indicador de meta por identificador.
    /// </summary>
    /// <response code="200">Indicador encontrado.</response>
    /// <response code="404">Indicador não encontrado.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(IndicatorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Indicator>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await getIndicatorById.ExecuteAsync(id, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Lista indicadores por meta com paginação.
    /// </summary>
    /// <response code="200">Lista paginada retornada com sucesso.</response>
    /// <response code="400">Parâmetros inválidos.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Indicator>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<Indicator>>> GetAll(
        [FromQuery] Guid? goalId,
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

        var result = await listIndicators.ExecuteAsync(goalId, searchValidation.Value, page, pageSize, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Retorna o progresso de um indicador.
    /// </summary>
    /// <response code="200">Progresso calculado com sucesso.</response>
    /// <response code="404">Indicador não encontrado.</response>
    [HttpGet("{id:guid}/progress")]
    [ProducesResponseType(typeof(IndicatorProgressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IndicatorProgressResponse?>> GetProgress(Guid id, CancellationToken cancellationToken)
    {
        var result = await getIndicatorProgress.ExecuteAsync(id, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Cria um check-in para um indicador.
    /// </summary>
    [HttpPost("{indicatorId:guid}/checkins")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(CheckinResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Checkin>> CreateCheckinAction(
        Guid indicatorId,
        CreateCheckinRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await createCheckinValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await createCheckin.ExecuteAsync(User, indicatorId, request, cancellationToken);
        return FromResult(result, checkin =>
            CreatedAtAction(nameof(GetCheckinById), new { indicatorId, checkinId = checkin.Id }, checkin));
    }

    /// <summary>
    /// Lista check-ins de um indicador com paginação.
    /// </summary>
    [HttpGet("{indicatorId:guid}/checkins")]
    [ProducesResponseType(typeof(PagedResult<Checkin>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<Checkin>>> GetCheckins(
        Guid indicatorId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var paginationValidation = ValidatePagination(page, pageSize);
        if (paginationValidation is not null)
        {
            return paginationValidation;
        }

        var result = await listCheckins.ExecuteAsync(indicatorId, page, pageSize, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Busca um check-in por identificador dentro do indicador.
    /// </summary>
    [HttpGet("{indicatorId:guid}/checkins/{checkinId:guid}")]
    [ProducesResponseType(typeof(CheckinResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Checkin>> GetCheckinById(
        Guid indicatorId,
        Guid checkinId,
        CancellationToken cancellationToken)
    {
        var result = await getCheckinById.ExecuteAsync(indicatorId, checkinId, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Atualiza um check-in dentro do indicador.
    /// </summary>
    [HttpPatch("{indicatorId:guid}/checkins/{checkinId:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(CheckinResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Checkin>> PatchCheckinAction(
        Guid indicatorId,
        Guid checkinId,
        PatchCheckinRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await patchCheckinValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await patchCheckin.ExecuteAsync(User, indicatorId, checkinId, request, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Exclui um check-in dentro do indicador.
    /// </summary>
    [HttpDelete("{indicatorId:guid}/checkins/{checkinId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteCheckinAction(
        Guid indicatorId,
        Guid checkinId,
        CancellationToken cancellationToken)
    {
        var result = await deleteCheckin.ExecuteAsync(User, indicatorId, checkinId, cancellationToken);
        return FromResult(result, NoContent);
    }
}
