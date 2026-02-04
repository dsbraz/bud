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
[Route("api/teams")]
public sealed class TeamsController(
    ITeamService teamService,
    ApplicationDbContext dbContext,
    IAuthorizationService authorizationService,
    IValidator<CreateTeamRequest> createValidator,
    IValidator<UpdateTeamRequest> updateValidator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(Team), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Team>> Create(CreateTeamRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(
                validationResult.ToDictionary()));
        }

        var workspace = await dbContext.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId, cancellationToken);

        if (workspace is null)
        {
            return NotFound(new ProblemDetails { Detail = "Workspace não encontrado." });
        }

        var authResult = await authorizationService.AuthorizeAsync(
            User,
            new OrganizationResource(workspace.OrganizationId),
            AuthorizationPolicies.OrganizationOwner);

        if (!authResult.Succeeded)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Acesso negado",
                Detail = "Apenas o proprietário da organização pode criar times."
            });
        }

        var result = await teamService.CreateAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            return result.ErrorType switch
            {
                ServiceErrorType.NotFound => NotFound(new ProblemDetails { Detail = result.Error }),
                ServiceErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden,
                    new ProblemDetails { Detail = result.Error }),
                _ => BadRequest(new ProblemDetails { Detail = result.Error })
            };
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Team), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Team>> Update(Guid id, UpdateTeamRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(
                validationResult.ToDictionary()));
        }

        var team = await dbContext.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (team is null)
        {
            return NotFound(new ProblemDetails { Detail = "Time não encontrado." });
        }

        var authResult = await authorizationService.AuthorizeAsync(
            User,
            new OrganizationResource(team.OrganizationId),
            AuthorizationPolicies.OrganizationWrite);

        if (!authResult.Succeeded)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Acesso negado",
                Detail = "Você não tem permissão para atualizar este time."
            });
        }

        var result = await teamService.UpdateAsync(id, request, cancellationToken);

        if (result.IsFailure)
        {
            return result.ErrorType switch
            {
                ServiceErrorType.NotFound => NotFound(new ProblemDetails { Detail = result.Error }),
                ServiceErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden,
                    new ProblemDetails { Detail = result.Error }),
                _ => BadRequest(new ProblemDetails { Detail = result.Error })
            };
        }

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var team = await dbContext.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (team is null)
        {
            return NotFound(new ProblemDetails { Detail = "Time não encontrado." });
        }

        var authResult = await authorizationService.AuthorizeAsync(
            User,
            new OrganizationResource(team.OrganizationId),
            AuthorizationPolicies.OrganizationWrite);

        if (!authResult.Succeeded)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Acesso negado",
                Detail = "Você não tem permissão para excluir este time."
            });
        }

        var result = await teamService.DeleteAsync(id, cancellationToken);

        if (result.IsFailure)
        {
            return result.ErrorType switch
            {
                ServiceErrorType.NotFound => NotFound(new ProblemDetails { Detail = result.Error }),
                ServiceErrorType.Conflict => Conflict(new ProblemDetails { Detail = result.Error }),
                ServiceErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden,
                    new ProblemDetails { Detail = result.Error }),
                _ => BadRequest(new ProblemDetails { Detail = result.Error })
            };
        }

        return NoContent();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Team), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Team>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await teamService.GetByIdAsync(id, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new ProblemDetails { Detail = result.Error });
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Team>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<Team>>> GetAll(
        [FromQuery] Guid? workspaceId,
        [FromQuery] Guid? parentTeamId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await teamService.GetAllAsync(workspaceId, parentTeamId, search, page, pageSize, cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}/subteams")]
    [ProducesResponseType(typeof(PagedResult<Team>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<Team>>> GetSubTeams(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await teamService.GetSubTeamsAsync(id, page, pageSize, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new ProblemDetails { Detail = result.Error });
    }

    [HttpGet("{id:guid}/collaborators")]
    [ProducesResponseType(typeof(PagedResult<Collaborator>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<Collaborator>>> GetCollaborators(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await teamService.GetCollaboratorsAsync(id, page, pageSize, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new ProblemDetails { Detail = result.Error });
    }

    [HttpGet("{id:guid}/collaborators-summary")]
    [ProducesResponseType(typeof(List<CollaboratorSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<CollaboratorSummaryDto>>> GetCollaboratorSummaries(Guid id, CancellationToken cancellationToken)
    {
        var result = await teamService.GetCollaboratorSummariesAsync(id, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new ProblemDetails { Detail = result.Error });
    }

    [HttpPut("{id:guid}/collaborators")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateCollaborators(Guid id, UpdateTeamCollaboratorsRequest request, CancellationToken cancellationToken)
    {
        var team = await dbContext.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (team is null)
        {
            return NotFound(new ProblemDetails { Detail = "Time não encontrado." });
        }

        var authResult = await authorizationService.AuthorizeAsync(
            User,
            new OrganizationResource(team.OrganizationId),
            AuthorizationPolicies.OrganizationOwner);

        if (!authResult.Succeeded)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Acesso negado",
                Detail = "Apenas o proprietário da organização pode atribuir colaboradores."
            });
        }

        var result = await teamService.UpdateCollaboratorsAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ErrorType == ServiceErrorType.NotFound
                ? NotFound(new ProblemDetails { Detail = result.Error })
                : BadRequest(new ProblemDetails { Detail = result.Error });
    }

    [HttpGet("{id:guid}/available-collaborators")]
    [ProducesResponseType(typeof(List<CollaboratorSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<CollaboratorSummaryDto>>> GetAvailableCollaborators(
        Guid id,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var result = await teamService.GetAvailableCollaboratorsAsync(id, search, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new ProblemDetails { Detail = result.Error });
    }
}
