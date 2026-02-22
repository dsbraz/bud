using Bud.Server.Application.UseCases.Notifications;
using Bud.Server.Authorization;
using Bud.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bud.Server.Domain.Model;

namespace Bud.Server.Controllers;

/// <summary>
/// Gerencia notificações do colaborador autenticado.
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.TenantSelected)]
[Route("api/notifications")]
[Produces("application/json")]
public sealed class NotificationsController(
    ListNotifications listNotifications,
    PatchNotification patchNotification,
    PatchNotifications patchNotifications) : ApiControllerBase
{
    /// <summary>
    /// Lista notificações do colaborador autenticado com paginação.
    /// </summary>
    /// <response code="200">Lista paginada retornada com sucesso.</response>
    /// <response code="400">Parâmetros de paginação inválidos.</response>
    /// <response code="403">Colaborador não identificado.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NotificationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<NotificationResponse>>> GetAll(
        [FromQuery] bool? isRead,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var paginationValidation = ValidatePagination(page, pageSize);
        if (paginationValidation is not null)
        {
            return paginationValidation;
        }

        var result = await listNotifications.ExecuteAsync(isRead, page, pageSize, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Marca uma notificação como lida.
    /// </summary>
    /// <response code="204">Notificação marcada como lida.</response>
    /// <response code="403">Sem permissão para marcar esta notificação.</response>
    /// <response code="404">Notificação não encontrada.</response>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        var result = await patchNotification.ExecuteAsync(id, cancellationToken);
        return FromResult(result, NoContent);
    }

    /// <summary>
    /// Marca todas as notificações do colaborador autenticado como lidas.
    /// </summary>
    /// <response code="204">Todas as notificações marcadas como lidas.</response>
    /// <response code="403">Colaborador não identificado.</response>
    [HttpPatch]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var result = await patchNotifications.ExecuteAsync(cancellationToken);
        return FromResult(result, NoContent);
    }
}
