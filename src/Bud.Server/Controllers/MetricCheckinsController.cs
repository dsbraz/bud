using Bud.Server.Authorization;
using Bud.Server.Authorization.ResourceScopes;
using Bud.Server.Data;
using Bud.Server.MultiTenancy;
using Bud.Server.Services;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.TenantSelected)]
[Route("api/metric-checkins")]
public sealed class MetricCheckinsController(
    IMetricCheckinService checkinService,
    ApplicationDbContext dbContext,
    IAuthorizationService authorizationService,
    ITenantProvider tenantProvider,
    IValidator<CreateMetricCheckinRequest> createValidator,
    IValidator<UpdateMetricCheckinRequest> updateValidator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(MetricCheckin), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MetricCheckin>> Create(CreateMetricCheckinRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(
                validationResult.ToDictionary()));
        }

        var metric = await dbContext.MissionMetrics
            .AsNoTracking()
            .Include(m => m.Mission)
            .FirstOrDefaultAsync(m => m.Id == request.MissionMetricId, cancellationToken);

        if (metric is null)
        {
            return NotFound(new ProblemDetails { Detail = "Métrica não encontrada." });
        }

        var tenantAuth = await authorizationService.AuthorizeAsync(
            User,
            new OrganizationResource(metric.OrganizationId),
            AuthorizationPolicies.TenantOrganizationMatch);

        if (!tenantAuth.Succeeded)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Acesso negado",
                Detail = "Você não tem permissão para criar check-ins nesta métrica."
            });
        }

        var mission = metric.Mission;
        var scopeAuth = await authorizationService.AuthorizeAsync(
            User,
            new MissionScopeResource(mission.WorkspaceId, mission.TeamId, mission.CollaboratorId),
            AuthorizationPolicies.MissionScopeAccess);

        if (!scopeAuth.Succeeded)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Acesso negado",
                Detail = "Você não tem permissão para fazer check-in nesta métrica."
            });
        }

        var collaboratorId = tenantProvider.CollaboratorId;
        if (!collaboratorId.HasValue)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Acesso negado",
                Detail = "Colaborador não identificado."
            });
        }

        var result = await checkinService.CreateAsync(request, collaboratorId.Value, cancellationToken);

        if (result.IsFailure)
        {
            return result.ErrorType == ServiceErrorType.NotFound
                ? NotFound(new ProblemDetails { Detail = result.Error })
                : BadRequest(new ProblemDetails { Detail = result.Error });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(MetricCheckin), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MetricCheckin>> Update(Guid id, UpdateMetricCheckinRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(
                validationResult.ToDictionary()));
        }

        var checkin = await dbContext.MetricCheckins
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(mc => mc.Id == id, cancellationToken);

        if (checkin is null)
        {
            return NotFound(new ProblemDetails { Detail = "Check-in não encontrado." });
        }

        var tenantAuth = await authorizationService.AuthorizeAsync(
            User,
            new OrganizationResource(checkin.OrganizationId),
            AuthorizationPolicies.TenantOrganizationMatch);

        if (!tenantAuth.Succeeded)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Acesso negado",
                Detail = "Você não tem permissão para atualizar este check-in."
            });
        }

        if (!tenantProvider.IsGlobalAdmin && tenantProvider.CollaboratorId != checkin.CollaboratorId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Acesso negado",
                Detail = "Apenas o autor pode editar este check-in."
            });
        }

        var result = await checkinService.UpdateAsync(id, request, cancellationToken);

        if (result.IsFailure)
        {
            return result.ErrorType == ServiceErrorType.NotFound
                ? NotFound(new ProblemDetails { Detail = result.Error })
                : BadRequest(new ProblemDetails { Detail = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var checkin = await dbContext.MetricCheckins
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(mc => mc.Id == id, cancellationToken);

        if (checkin is null)
        {
            return NotFound(new ProblemDetails { Detail = "Check-in não encontrado." });
        }

        var tenantAuth = await authorizationService.AuthorizeAsync(
            User,
            new OrganizationResource(checkin.OrganizationId),
            AuthorizationPolicies.TenantOrganizationMatch);

        if (!tenantAuth.Succeeded)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Acesso negado",
                Detail = "Você não tem permissão para excluir este check-in."
            });
        }

        if (!tenantProvider.IsGlobalAdmin && tenantProvider.CollaboratorId != checkin.CollaboratorId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Acesso negado",
                Detail = "Apenas o autor pode excluir este check-in."
            });
        }

        var result = await checkinService.DeleteAsync(id, cancellationToken);

        if (result.IsFailure)
        {
            return result.ErrorType == ServiceErrorType.NotFound
                ? NotFound(new ProblemDetails { Detail = result.Error })
                : BadRequest(new ProblemDetails { Detail = result.Error });
        }

        return NoContent();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MetricCheckin), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MetricCheckin>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await checkinService.GetByIdAsync(id, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new ProblemDetails { Detail = result.Error });
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<MetricCheckin>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MetricCheckin>>> GetAll(
        [FromQuery] Guid? missionMetricId,
        [FromQuery] Guid? missionId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await checkinService.GetAllAsync(missionMetricId, missionId, page, pageSize, cancellationToken);
        return Ok(result.Value);
    }

}
