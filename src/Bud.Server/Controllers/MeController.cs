using System.Security.Claims;
using Bud.Server.Application.UseCases.Me;
using Bud.Server.Application.UseCases.Missions;
using Bud.Server.Authorization;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bud.Server.Domain.Model;

namespace Bud.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
[Produces("application/json")]
public sealed class MeController(
    ListMyOrganizations listMyOrganizations,
    GetMyDashboard getMyDashboard,
    ListCollaboratorMissions listCollaboratorMissions,
    ITenantProvider tenantProvider) : ApiControllerBase
{
    /// <summary>
    /// Lista organizações disponíveis para o usuário autenticado.
    /// </summary>
    [HttpGet("organizations")]
    [ProducesResponseType(typeof(List<OrganizationSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<OrganizationSummaryResponse>>> GetOrganizations(CancellationToken cancellationToken)
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

    /// <summary>
    /// Lista missões do colaborador autenticado.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.TenantSelected)]
    [HttpGet("missions")]
    [ProducesResponseType(typeof(PagedResult<Mission>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<Mission>>> GetMissions(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var collaboratorId = tenantProvider.CollaboratorId;
        if (!collaboratorId.HasValue)
        {
            return ForbiddenProblem("Colaborador não identificado.");
        }

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

        var result = await listCollaboratorMissions.ExecuteAsync(
            collaboratorId.Value,
            searchValidation.Value,
            page,
            pageSize,
            cancellationToken);
        return FromResultOk(result);
    }
}
