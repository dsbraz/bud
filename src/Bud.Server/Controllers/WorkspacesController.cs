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
[Route("api/workspaces")]
public sealed class WorkspacesController(
    IWorkspaceService workspaceService,
    ApplicationDbContext dbContext,
    IAuthorizationService authorizationService,
    IValidator<CreateWorkspaceRequest> createValidator,
    IValidator<UpdateWorkspaceRequest> updateValidator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(Workspace), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Workspace>> Create(CreateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(
                validationResult.ToDictionary()));
        }

        var authResult = await authorizationService.AuthorizeAsync(
            User,
            new OrganizationResource(request.OrganizationId),
            AuthorizationPolicies.OrganizationOwner);

        if (!authResult.Succeeded)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Acesso negado",
                Detail = "Apenas o proprietário da organização pode criar workspaces."
            });
        }

        var result = await workspaceService.CreateAsync(request, cancellationToken);

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
    [ProducesResponseType(typeof(Workspace), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Workspace>> Update(Guid id, UpdateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(
                validationResult.ToDictionary()));
        }

        var workspace = await dbContext.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (workspace is null)
        {
            return NotFound(new ProblemDetails { Detail = "Workspace não encontrado." });
        }

        var authResult = await authorizationService.AuthorizeAsync(
            User,
            new OrganizationResource(workspace.OrganizationId),
            AuthorizationPolicies.OrganizationWrite);

        if (!authResult.Succeeded)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Acesso negado",
                Detail = "Você não tem permissão para atualizar este workspace."
            });
        }

        var result = await workspaceService.UpdateAsync(id, request, cancellationToken);

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
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var workspace = await dbContext.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (workspace is null)
        {
            return NotFound(new ProblemDetails { Detail = "Workspace não encontrado." });
        }

        var authResult = await authorizationService.AuthorizeAsync(
            User,
            new OrganizationResource(workspace.OrganizationId),
            AuthorizationPolicies.OrganizationWrite);

        if (!authResult.Succeeded)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Acesso negado",
                Detail = "Você não tem permissão para excluir este workspace."
            });
        }

        var result = await workspaceService.DeleteAsync(id, cancellationToken);

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

        return NoContent();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Workspace), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Workspace>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await workspaceService.GetByIdAsync(id, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new ProblemDetails { Detail = result.Error });
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Workspace>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<Workspace>>> GetAll(
        [FromQuery] Guid? organizationId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await workspaceService.GetAllAsync(organizationId, search, page, pageSize, cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}/teams")]
    [ProducesResponseType(typeof(PagedResult<Team>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<Team>>> GetTeams(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await workspaceService.GetTeamsAsync(id, page, pageSize, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new ProblemDetails { Detail = result.Error });
    }
}
