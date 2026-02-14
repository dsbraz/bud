using Bud.Server.Application.Dashboard;
using Bud.Server.Authorization;
using Bud.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bud.Server.Controllers;

/// <summary>
/// Endpoints do dashboard pessoal.
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = AuthorizationPolicies.TenantSelected)]
[Produces("application/json")]
public sealed class DashboardController(IDashboardQueryUseCase dashboardQueryUseCase) : ApiControllerBase
{
    /// <summary>
    /// Retorna os dados do dashboard do colaborador.
    /// </summary>
    [HttpGet("my-dashboard")]
    [ProducesResponseType(typeof(MyDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MyDashboardResponse>> GetMyDashboard(
        [FromQuery] Guid? teamId,
        CancellationToken cancellationToken)
    {
        var result = await dashboardQueryUseCase.GetMyDashboardAsync(User, teamId, cancellationToken);
        return FromResultOk(result);
    }
}
