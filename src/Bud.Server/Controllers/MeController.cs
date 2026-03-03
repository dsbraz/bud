using System.Security.Claims;
using Bud.Server.Application.UseCases.Me;
using Bud.Server.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bud.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
[Produces("application/json")]
public sealed class MeController(
    ListMyOrganizations listMyOrganizations,
    GetMyDashboard getMyDashboard) : ApiControllerBase
{
    /// <summary>
    /// Lista organizações disponíveis para o usuário autenticado.
    /// </summary>
    [HttpGet("organizations")]
    [ProducesResponseType(typeof(List<MyOrganizationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<MyOrganizationResponse>>> GetOrganizations(CancellationToken cancellationToken)
    {
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new ProblemDetails { Detail = "Não foi possível identificar o e-mail do usuário autenticado." });
        }

        var result = await listMyOrganizations.ExecuteAsync(email, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Retorna os dados do dashboard do colaborador autenticado.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.TenantSelected)]
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(MyDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MyDashboardResponse>> GetDashboard(
        [FromQuery] Guid? teamId,
        CancellationToken cancellationToken)
    {
        var result = await getMyDashboard.ExecuteAsync(User, teamId, cancellationToken);
        return FromResultOk(result);
    }
}
