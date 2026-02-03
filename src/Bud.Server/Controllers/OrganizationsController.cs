using Bud.Server.MultiTenancy;
using Bud.Server.Services;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Bud.Server.Controllers;

[ApiController]
[Route("api/organizations")]
public sealed class OrganizationsController(
    IOrganizationService organizationService,
    IValidator<CreateOrganizationRequest> createValidator,
    IValidator<UpdateOrganizationRequest> updateValidator,
    ITenantProvider tenantProvider) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(Organization), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Organization>> Create(CreateOrganizationRequest request, CancellationToken cancellationToken)
    {
        // Apenas administradores podem criar organizações
        if (!tenantProvider.IsAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Acesso negado",
                    Detail = "Apenas administradores podem criar organizações."
                });
        }

        var validationResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(
                validationResult.ToDictionary()));
        }

        var result = await organizationService.CreateAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            return result.ErrorType switch
            {
                ServiceErrorType.NotFound => NotFound(new ProblemDetails { Detail = result.Error }),
                ServiceErrorType.Validation => BadRequest(new ProblemDetails { Detail = result.Error }),
                _ => BadRequest(new ProblemDetails { Detail = result.Error })
            };
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Organization), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Organization>> Update(Guid id, UpdateOrganizationRequest request, CancellationToken cancellationToken)
    {
        // Apenas administradores podem atualizar organizações
        if (!tenantProvider.IsAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Acesso negado",
                    Detail = "Apenas administradores podem atualizar organizações."
                });
        }

        var validationResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(
                validationResult.ToDictionary()));
        }

        var result = await organizationService.UpdateAsync(id, request, cancellationToken);

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
        // Apenas administradores podem deletar organizações
        if (!tenantProvider.IsAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Acesso negado",
                    Detail = "Apenas administradores podem deletar organizações."
                });
        }

        var result = await organizationService.DeleteAsync(id, cancellationToken);

        if (result.IsFailure)
        {
            return result.ErrorType == ServiceErrorType.NotFound
                ? NotFound(new ProblemDetails { Detail = result.Error })
                : BadRequest(new ProblemDetails { Detail = result.Error });
        }

        return NoContent();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Organization), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Organization>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await organizationService.GetByIdAsync(id, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new ProblemDetails { Detail = result.Error });
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Organization>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<Organization>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await organizationService.GetAllAsync(search, page, pageSize, cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}/workspaces")]
    [ProducesResponseType(typeof(PagedResult<Workspace>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<Workspace>>> GetWorkspaces(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await organizationService.GetWorkspacesAsync(id, page, pageSize, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new ProblemDetails { Detail = result.Error });
    }
}
