using Bud.Server.Application.Outbox;
using Bud.Server.Authorization;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bud.Server.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.GlobalAdmin)]
[Route("api/outbox")]
[Produces("application/json")]
public sealed class OutboxController(
    IOutboxQueryUseCase outboxQueryUseCase,
    IOutboxCommandUseCase outboxCommandUseCase) : ApiControllerBase
{
    /// <summary>
    /// Lista mensagens do Outbox em dead-letter.
    /// </summary>
    /// <response code="200">Lista paginada retornada com sucesso.</response>
    /// <response code="400">Parâmetros de paginação inválidos.</response>
    [HttpGet("dead-letters")]
    [ProducesResponseType(typeof(PagedResult<OutboxDeadLetterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<OutboxDeadLetterDto>>> GetDeadLetters(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var paginationValidation = ValidatePagination(page, pageSize);
        if (paginationValidation is not null)
        {
            return paginationValidation;
        }

        var result = await outboxQueryUseCase.GetDeadLettersAsync(page, pageSize, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Reprocessa uma mensagem específica em dead-letter.
    /// </summary>
    /// <response code="204">Mensagem reprocessada com sucesso.</response>
    /// <response code="404">Mensagem não encontrada.</response>
    /// <response code="400">Não foi possível reprocessar a mensagem.</response>
    [HttpPost("dead-letters/{id:guid}/reprocess")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReprocessDeadLetter(Guid id, CancellationToken cancellationToken)
    {
        var result = await outboxCommandUseCase.ReprocessDeadLetterAsync(id, cancellationToken);
        return FromResult(result, NoContent);
    }

    /// <summary>
    /// Reprocessa mensagens em dead-letter com filtros.
    /// </summary>
    /// <remarks>
    /// Exemplo de payload:
    /// { "eventType": "Bud.Server.Domain.Missions.Events.MissionUpdatedDomainEvent", "maxItems": 50 }
    /// </remarks>
    /// <response code="200">Reprocessamento concluído.</response>
    /// <response code="400">Payload inválido ou filtro inconsistente.</response>
    [HttpPost("dead-letters/reprocess")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ReprocessDeadLettersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReprocessDeadLettersResponse>> ReprocessDeadLetters(
        [FromBody] ReprocessDeadLettersRequest request,
        CancellationToken cancellationToken)
    {
        var result = await outboxCommandUseCase.ReprocessDeadLettersAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(new ReprocessDeadLettersResponse { ReprocessedCount = result.Value });
        }

        return result.ErrorType switch
        {
            ServiceErrorType.NotFound => NotFound(new ProblemDetails { Detail = result.Error }),
            ServiceErrorType.Conflict => Conflict(new ProblemDetails { Detail = result.Error }),
            ServiceErrorType.Forbidden => ForbiddenProblem(result.Error ?? "Você não tem permissão para realizar esta ação."),
            _ => BadRequest(new ProblemDetails { Detail = result.Error })
        };
    }
}
