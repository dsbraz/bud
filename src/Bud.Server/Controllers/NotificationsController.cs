using Bud.Server.Application.Notifications;
using Bud.Server.Authorization;
using Bud.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    GetUnreadNotificationCount getUnreadNotificationCount,
    MarkNotificationAsRead markNotificationAsRead,
    MarkAllNotificationsAsRead markAllNotificationsAsRead) : ApiControllerBase
{
    /// <summary>
    /// Lista notificações do colaborador autenticado com paginação.
    /// </summary>
    /// <response code="200">Lista paginada retornada com sucesso.</response>
    /// <response code="400">Parâmetros de paginação inválidos.</response>
    /// <response code="403">Colaborador não identificado.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<NotificationDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var paginationValidation = ValidatePagination(page, pageSize);
        if (paginationValidation is not null)
        {
            return paginationValidation;
        }

        var result = await listNotifications.ExecuteAsync(page, pageSize, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Retorna a contagem de notificações não lidas do colaborador autenticado.
    /// </summary>
    /// <response code="200">Contagem retornada com sucesso.</response>
    /// <response code="403">Colaborador não identificado.</response>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(UnreadCountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UnreadCountResponse>> GetUnreadCount(CancellationToken cancellationToken)
    {
        var result = await getUnreadNotificationCount.ExecuteAsync(cancellationToken);
        return FromResultOk(result, count => new UnreadCountResponse { Count = count });
    }

    /// <summary>
    /// Marca uma notificação como lida.
    /// </summary>
    /// <response code="204">Notificação marcada como lida.</response>
    /// <response code="403">Sem permissão para marcar esta notificação.</response>
    /// <response code="404">Notificação não encontrada.</response>
    [HttpPut("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        var result = await markNotificationAsRead.ExecuteAsync(id, cancellationToken);
        return FromResult(result, NoContent);
    }

    /// <summary>
    /// Marca todas as notificações do colaborador autenticado como lidas.
    /// </summary>
    /// <response code="204">Todas as notificações marcadas como lidas.</response>
    /// <response code="403">Colaborador não identificado.</response>
    [HttpPut("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var result = await markAllNotificationsAsRead.ExecuteAsync(cancellationToken);
        return FromResult(result, NoContent);
    }
}
