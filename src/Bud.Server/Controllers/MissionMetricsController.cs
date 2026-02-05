using Bud.Server.Authorization;
using Bud.Server.Authorization.ResourceScopes;
using Bud.Server.Data;
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
[Route("api/mission-metrics")]
public sealed class MissionMetricsController(
    IMissionMetricService metricService,
    IMissionProgressService missionProgressService,
    ApplicationDbContext dbContext,
    IAuthorizationService authorizationService,
    IValidator<CreateMissionMetricRequest> createValidator,
    IValidator<UpdateMissionMetricRequest> updateValidator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(MissionMetric), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MissionMetric>> Create(CreateMissionMetricRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(
                validationResult.ToDictionary()));
        }

        var mission = await dbContext.Missions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.MissionId, cancellationToken);

        if (mission is null)
        {
            return NotFound(new ProblemDetails { Detail = "Missão não encontrada." });
        }

        var authResult = await authorizationService.AuthorizeAsync(
            User,
            new OrganizationResource(mission.OrganizationId),
            AuthorizationPolicies.TenantOrganizationMatch);

        if (!authResult.Succeeded)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Acesso negado",
                Detail = "Você não tem permissão para criar métricas nesta missão."
            });
        }

        var result = await metricService.CreateAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            return result.ErrorType == ServiceErrorType.NotFound
                ? NotFound(new ProblemDetails { Detail = result.Error })
                : BadRequest(new ProblemDetails { Detail = result.Error });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(MissionMetric), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MissionMetric>> Update(Guid id, UpdateMissionMetricRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(
                validationResult.ToDictionary()));
        }

        var metric = await dbContext.MissionMetrics
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (metric is null)
        {
            return NotFound(new ProblemDetails { Detail = "Métrica da missão não encontrada." });
        }

        var authResult = await authorizationService.AuthorizeAsync(
            User,
            new OrganizationResource(metric.OrganizationId),
            AuthorizationPolicies.TenantOrganizationMatch);

        if (!authResult.Succeeded)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Acesso negado",
                Detail = "Você não tem permissão para atualizar métricas nesta missão."
            });
        }

        var result = await metricService.UpdateAsync(id, request, cancellationToken);

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
        var metric = await dbContext.MissionMetrics
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (metric is null)
        {
            return NotFound(new ProblemDetails { Detail = "Métrica da missão não encontrada." });
        }

        var authResult = await authorizationService.AuthorizeAsync(
            User,
            new OrganizationResource(metric.OrganizationId),
            AuthorizationPolicies.TenantOrganizationMatch);

        if (!authResult.Succeeded)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Acesso negado",
                Detail = "Você não tem permissão para excluir métricas nesta missão."
            });
        }

        var result = await metricService.DeleteAsync(id, cancellationToken);

        if (result.IsFailure)
        {
            return result.ErrorType == ServiceErrorType.NotFound
                ? NotFound(new ProblemDetails { Detail = result.Error })
                : BadRequest(new ProblemDetails { Detail = result.Error });
        }

        return NoContent();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MissionMetric), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MissionMetric>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await metricService.GetByIdAsync(id, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new ProblemDetails { Detail = result.Error });
    }

    [HttpGet("progress")]
    [ProducesResponseType(typeof(List<MetricProgressDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MetricProgressDto>>> GetProgress(
        [FromQuery] string ids,
        CancellationToken cancellationToken)
    {
        var metricIds = new List<Guid>();
        foreach (var part in (ids ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Guid.TryParse(part, out var id))
            {
                metricIds.Add(id);
            }
        }

        var result = await missionProgressService.GetMetricProgressAsync(metricIds, cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<MissionMetric>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MissionMetric>>> GetAll(
        [FromQuery] Guid? missionId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await metricService.GetAllAsync(missionId, search, page, pageSize, cancellationToken);
        return Ok(result.Value);
    }
}
