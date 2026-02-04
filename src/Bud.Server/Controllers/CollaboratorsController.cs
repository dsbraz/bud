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
[Route("api/collaborators")]
public sealed class CollaboratorsController(
    ICollaboratorService collaboratorService,
    ApplicationDbContext dbContext,
    IAuthorizationService authorizationService,
    ITenantProvider tenantProvider,
    IValidator<CreateCollaboratorRequest> createValidator,
    IValidator<UpdateCollaboratorRequest> updateValidator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(Collaborator), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Collaborator>> Create(CreateCollaboratorRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(
                validationResult.ToDictionary()));
        }

        if (tenantProvider.TenantId.HasValue)
        {
            var authResult = await authorizationService.AuthorizeAsync(
                User,
                new OrganizationResource(tenantProvider.TenantId.Value),
                AuthorizationPolicies.OrganizationOwner);

            if (!authResult.Succeeded)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Title = "Acesso negado",
                    Detail = "Apenas o proprietário da organização pode criar colaboradores."
                });
            }
        }

        var result = await collaboratorService.CreateAsync(request, cancellationToken);

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
    [ProducesResponseType(typeof(Collaborator), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Collaborator>> Update(Guid id, UpdateCollaboratorRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(
                validationResult.ToDictionary()));
        }

        var collaborator = await dbContext.Collaborators
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (collaborator is null)
        {
            return NotFound(new ProblemDetails { Detail = "Colaborador não encontrado." });
        }

        var authResult = await authorizationService.AuthorizeAsync(
            User,
            new OrganizationResource(collaborator.OrganizationId),
            AuthorizationPolicies.OrganizationOwner);

        if (!authResult.Succeeded)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Acesso negado",
                Detail = "Apenas o proprietário da organização pode editar colaboradores."
            });
        }

        var result = await collaboratorService.UpdateAsync(id, request, cancellationToken);

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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var collaborator = await dbContext.Collaborators
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (collaborator is null)
        {
            return NotFound(new ProblemDetails { Detail = "Colaborador não encontrado." });
        }

        var authResult = await authorizationService.AuthorizeAsync(
            User,
            new OrganizationResource(collaborator.OrganizationId),
            AuthorizationPolicies.OrganizationOwner);

        if (!authResult.Succeeded)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Acesso negado",
                Detail = "Apenas o proprietário da organização pode excluir colaboradores."
            });
        }

        var result = await collaboratorService.DeleteAsync(id, cancellationToken);

        if (result.IsFailure)
        {
            return result.ErrorType switch
            {
                ServiceErrorType.NotFound => NotFound(new ProblemDetails { Detail = result.Error }),
                ServiceErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden,
                    new ProblemDetails { Detail = result.Error }),
                ServiceErrorType.Conflict => Conflict(new ProblemDetails { Detail = result.Error }),
                _ => BadRequest(new ProblemDetails { Detail = result.Error })
            };
        }

        return NoContent();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Collaborator), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Collaborator>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await collaboratorService.GetByIdAsync(id, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new ProblemDetails { Detail = result.Error });
    }

    [HttpGet("leaders")]
    [ProducesResponseType(typeof(List<LeaderCollaboratorResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<LeaderCollaboratorResponse>>> GetLeaders(
        [FromQuery] Guid? organizationId,
        CancellationToken cancellationToken)
    {
        var result = await collaboratorService.GetLeadersAsync(organizationId, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails { Detail = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Collaborator>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<Collaborator>>> GetAll(
        [FromQuery] Guid? teamId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await collaboratorService.GetAllAsync(teamId, search, page, pageSize, cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}/teams")]
    [ProducesResponseType(typeof(List<TeamSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<TeamSummaryDto>>> GetTeams(Guid id, CancellationToken cancellationToken)
    {
        var result = await collaboratorService.GetTeamsAsync(id, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new ProblemDetails { Detail = result.Error });
    }

    [HttpPut("{id:guid}/teams")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateTeams(Guid id, UpdateCollaboratorTeamsRequest request, CancellationToken cancellationToken)
    {
        var collaborator = await dbContext.Collaborators
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (collaborator is null)
        {
            return NotFound(new ProblemDetails { Detail = "Colaborador não encontrado." });
        }

        var authResult = await authorizationService.AuthorizeAsync(
            User,
            new OrganizationResource(collaborator.OrganizationId),
            AuthorizationPolicies.OrganizationOwner);

        if (!authResult.Succeeded)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Acesso negado",
                Detail = "Apenas o proprietário da organização pode atribuir equipes."
            });
        }

        var result = await collaboratorService.UpdateTeamsAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ErrorType == ServiceErrorType.NotFound
                ? NotFound(new ProblemDetails { Detail = result.Error })
                : BadRequest(new ProblemDetails { Detail = result.Error });
    }

    [HttpGet("{id:guid}/available-teams")]
    [ProducesResponseType(typeof(List<TeamSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<TeamSummaryDto>>> GetAvailableTeams(
        Guid id,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var result = await collaboratorService.GetAvailableTeamsAsync(id, search, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new ProblemDetails { Detail = result.Error });
    }
}
