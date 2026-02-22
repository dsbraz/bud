using Bud.Server.Application.Organizations;
using Bud.Server.Authorization;
using Bud.Shared.Contracts;
using Bud.Server.Domain.Model;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bud.Server.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.TenantSelected)]
[Route("api/organizations")]
[Produces("application/json")]
public sealed class OrganizationsController(
    CreateOrganization createOrganization,
    PatchOrganization patchOrganization,
    DeleteOrganization deleteOrganization,
    GetOrganizationById getOrganizationById,
    ListOrganizations listOrganizations,
    ListOrganizationWorkspaces listOrganizationWorkspaces,
    ListOrganizationCollaborators listOrganizationCollaborators,
    IValidator<CreateOrganizationRequest> createValidator,
    IValidator<PatchOrganizationRequest> updateValidator) : ApiControllerBase
{
    /// <summary>
    /// Cria uma organização.
    /// </summary>
    /// <response code="201">Organização criada com sucesso.</response>
    /// <response code="400">Payload inválido.</response>
    /// <response code="403">Acesso restrito a administrador global.</response>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.GlobalAdmin)]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(Organization), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Organization>> Create(CreateOrganizationRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await createOrganization.ExecuteAsync(request, cancellationToken);
        return FromResult(result, organization => CreatedAtAction(nameof(GetById), new { id = organization.Id }, organization));
    }

    /// <summary>
    /// Atualiza uma organização.
    /// </summary>
    /// <response code="200">Organização atualizada com sucesso.</response>
    /// <response code="400">Payload inválido.</response>
    /// <response code="404">Organização não encontrada.</response>
    /// <response code="403">Acesso restrito a administrador global.</response>
    [HttpPatch("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.GlobalAdmin)]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(Organization), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Organization>> Update(Guid id, PatchOrganizationRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await patchOrganization.ExecuteAsync(id, request, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Remove uma organização.
    /// </summary>
    /// <response code="204">Organização removida com sucesso.</response>
    /// <response code="404">Organização não encontrada.</response>
    /// <response code="409">Conflito de integridade ao remover organização.</response>
    /// <response code="403">Acesso restrito a administrador global.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.GlobalAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await deleteOrganization.ExecuteAsync(id, cancellationToken);
        return FromResult(result, NoContent);
    }

    /// <summary>
    /// Busca organização por identificador.
    /// </summary>
    /// <response code="200">Organização encontrada.</response>
    /// <response code="404">Organização não encontrada.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Organization), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Organization>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await getOrganizationById.ExecuteAsync(id, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Lista organizações com paginação e filtro de busca.
    /// </summary>
    /// <response code="200">Lista paginada retornada com sucesso.</response>
    /// <response code="400">Parâmetros inválidos.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Organization>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<Organization>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
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

        var result = await listOrganizations.ExecuteAsync(searchValidation.Value, page, pageSize, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Lista workspaces de uma organização.
    /// </summary>
    /// <response code="200">Lista paginada retornada com sucesso.</response>
    /// <response code="400">Parâmetros inválidos.</response>
    /// <response code="404">Organização não encontrada.</response>
    [HttpGet("{id:guid}/workspaces")]
    [ProducesResponseType(typeof(PagedResult<Workspace>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<Workspace>>> GetWorkspaces(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var paginationValidation = ValidatePagination(page, pageSize);
        if (paginationValidation is not null)
        {
            return paginationValidation;
        }

        var result = await listOrganizationWorkspaces.ExecuteAsync(id, page, pageSize, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Lista colaboradores de uma organização.
    /// </summary>
    /// <response code="200">Lista paginada retornada com sucesso.</response>
    /// <response code="400">Parâmetros inválidos.</response>
    /// <response code="404">Organização não encontrada.</response>
    [HttpGet("{id:guid}/collaborators")]
    [ProducesResponseType(typeof(PagedResult<Collaborator>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<Collaborator>>> GetCollaborators(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var paginationValidation = ValidatePagination(page, pageSize);
        if (paginationValidation is not null)
        {
            return paginationValidation;
        }

        var result = await listOrganizationCollaborators.ExecuteAsync(id, page, pageSize, cancellationToken);
        return FromResultOk(result);
    }
}
