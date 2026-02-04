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
[Route("api/missions")]
public sealed class MissionsController(
    IMissionService missionService,
    ApplicationDbContext dbContext,
    IAuthorizationService authorizationService,
    IValidator<CreateMissionRequest> createValidator,
    IValidator<UpdateMissionRequest> updateValidator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(Mission), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Mission>> Create(CreateMissionRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(
                validationResult.ToDictionary()));
        }

        var scopeExists = await ScopeExistsAsync(request.ScopeType, request.ScopeId, cancellationToken);
        if (!scopeExists.exists)
        {
            return NotFound(new ProblemDetails { Detail = scopeExists.error });
        }

        var organizationId = await ResolveOrganizationIdAsync(request.ScopeType, request.ScopeId, cancellationToken);
        if (organizationId is null)
        {
            return NotFound(new ProblemDetails { Detail = "Não foi possível determinar a organização para o escopo fornecido." });
        }

        var authResult = await authorizationService.AuthorizeAsync(
            User,
            new OrganizationResource(organizationId.Value),
            AuthorizationPolicies.TenantOrganizationMatch);

        if (!authResult.Succeeded)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Acesso negado",
                Detail = "Você não tem permissão para criar missões nesta organização."
            });
        }

        var result = await missionService.CreateAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            return result.ErrorType == ServiceErrorType.NotFound
                ? NotFound(new ProblemDetails { Detail = result.Error })
                : BadRequest(new ProblemDetails { Detail = result.Error });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Mission), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Mission>> Update(Guid id, UpdateMissionRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(
                validationResult.ToDictionary()));
        }

        var mission = await dbContext.Missions
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

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
                Detail = "Você não tem permissão para atualizar missões nesta organização."
            });
        }

        var result = await missionService.UpdateAsync(id, request, cancellationToken);

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
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var mission = await dbContext.Missions
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

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
                Detail = "Você não tem permissão para excluir missões nesta organização."
            });
        }

        var result = await missionService.DeleteAsync(id, cancellationToken);

        if (result.IsFailure)
        {
            return result.ErrorType == ServiceErrorType.NotFound
                ? NotFound(new ProblemDetails { Detail = result.Error })
                : BadRequest(new ProblemDetails { Detail = result.Error });
        }

        return NoContent();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Mission), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Mission>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await missionService.GetByIdAsync(id, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new ProblemDetails { Detail = result.Error });
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Mission>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<Mission>>> GetAll(
        [FromQuery] MissionScopeType? scopeType,
        [FromQuery] Guid? scopeId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await missionService.GetAllAsync(scopeType, scopeId, search, page, pageSize, cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("my-missions/{collaboratorId:guid}")]
    [ProducesResponseType(typeof(PagedResult<Mission>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<Mission>>> GetMyMissions(
        Guid collaboratorId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await missionService.GetMyMissionsAsync(collaboratorId, search, page, pageSize, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new ProblemDetails { Detail = result.Error });
    }

    [HttpGet("{id:guid}/metrics")]
    [ProducesResponseType(typeof(PagedResult<MissionMetric>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<MissionMetric>>> GetMetrics(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await missionService.GetMetricsAsync(id, page, pageSize, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new ProblemDetails { Detail = result.Error });
    }

    private async Task<(bool exists, string error)> ScopeExistsAsync(
        MissionScopeType scopeType,
        Guid scopeId,
        CancellationToken cancellationToken)
    {
        var exists = scopeType switch
        {
            MissionScopeType.Organization => await dbContext.Organizations.IgnoreQueryFilters().AnyAsync(o => o.Id == scopeId, cancellationToken),
            MissionScopeType.Workspace => await dbContext.Workspaces.IgnoreQueryFilters().AnyAsync(w => w.Id == scopeId, cancellationToken),
            MissionScopeType.Team => await dbContext.Teams.IgnoreQueryFilters().AnyAsync(t => t.Id == scopeId, cancellationToken),
            MissionScopeType.Collaborator => await dbContext.Collaborators.IgnoreQueryFilters().AnyAsync(c => c.Id == scopeId, cancellationToken),
            _ => false
        };

        if (exists)
        {
            return (true, string.Empty);
        }

        var message = scopeType switch
        {
            MissionScopeType.Organization => "Organização não encontrada.",
            MissionScopeType.Workspace => "Workspace não encontrado.",
            MissionScopeType.Team => "Time não encontrado.",
            MissionScopeType.Collaborator => "Colaborador não encontrado.",
            _ => "Escopo não encontrado."
        };

        return (false, message);
    }

    private async Task<Guid?> ResolveOrganizationIdAsync(
        MissionScopeType scopeType,
        Guid scopeId,
        CancellationToken cancellationToken)
    {
        return scopeType switch
        {
            MissionScopeType.Organization => scopeId,
            MissionScopeType.Workspace => await dbContext.Workspaces
                .IgnoreQueryFilters()
                .Where(w => w.Id == scopeId)
                .Select(w => (Guid?)w.OrganizationId)
                .FirstOrDefaultAsync(cancellationToken),
            MissionScopeType.Team => await dbContext.Teams
                .IgnoreQueryFilters()
                .Where(t => t.Id == scopeId)
                .Select(t => (Guid?)t.OrganizationId)
                .FirstOrDefaultAsync(cancellationToken),
            MissionScopeType.Collaborator => await dbContext.Collaborators
                .IgnoreQueryFilters()
                .Where(c => c.Id == scopeId)
                .Select(c => (Guid?)c.OrganizationId)
                .FirstOrDefaultAsync(cancellationToken),
            _ => null
        };
    }
}
