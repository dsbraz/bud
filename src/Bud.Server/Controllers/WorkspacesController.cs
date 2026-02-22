using Bud.Server.Application.UseCases.Workspaces;
using Bud.Server.Authorization;
using Bud.Shared.Contracts;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bud.Server.Domain.Model;

namespace Bud.Server.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.TenantSelected)]
[Route("api/workspaces")]
[Produces("application/json")]
public sealed class WorkspacesController(
    CreateWorkspace createWorkspace,
    PatchWorkspace patchWorkspace,
    DeleteWorkspace deleteWorkspace,
    GetWorkspaceById getWorkspaceById,
    ListWorkspaces listWorkspaces,
    ListWorkspaceTeams listWorkspaceTeams,
    IValidator<CreateWorkspaceRequest> createValidator,
    IValidator<PatchWorkspaceRequest> updateValidator) : ApiControllerBase
{
    /// <summary>
    /// Cria um workspace.
    /// </summary>
    /// <response code="201">Workspace criado com sucesso.</response>
    /// <response code="400">Payload inválido.</response>
    /// <response code="404">Organização não encontrada.</response>
    /// <response code="403">Sem permissão para criar workspace.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(WorkspaceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Workspace>> Create(CreateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await createWorkspace.ExecuteAsync(User, request, cancellationToken);
        return FromResult(result, workspace => CreatedAtAction(nameof(GetById), new { id = workspace.Id }, workspace));
    }

    /// <summary>
    /// Atualiza um workspace.
    /// </summary>
    /// <response code="200">Workspace atualizado com sucesso.</response>
    /// <response code="400">Payload inválido.</response>
    /// <response code="404">Workspace não encontrado.</response>
    /// <response code="403">Sem permissão para atualizar workspace.</response>
    [HttpPatch("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(WorkspaceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Workspace>> Update(Guid id, PatchWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await patchWorkspace.ExecuteAsync(User, id, request, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Exclui um workspace.
    /// </summary>
    /// <response code="204">Workspace removido com sucesso.</response>
    /// <response code="404">Workspace não encontrado.</response>
    /// <response code="403">Sem permissão para excluir workspace.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await deleteWorkspace.ExecuteAsync(User, id, cancellationToken);
        return FromResult(result, NoContent);
    }

    /// <summary>
    /// Busca workspace por identificador.
    /// </summary>
    /// <response code="200">Workspace encontrado.</response>
    /// <response code="404">Workspace não encontrado.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WorkspaceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Workspace>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await getWorkspaceById.ExecuteAsync(id, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Lista workspaces com paginação e filtro.
    /// </summary>
    /// <response code="200">Lista paginada retornada com sucesso.</response>
    /// <response code="400">Parâmetros inválidos.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Workspace>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<Workspace>>> GetAll(
        [FromQuery] Guid? organizationId,
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

        var result = await listWorkspaces.ExecuteAsync(organizationId, searchValidation.Value, page, pageSize, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Lista times de um workspace.
    /// </summary>
    /// <response code="200">Lista paginada retornada com sucesso.</response>
    /// <response code="400">Parâmetros inválidos.</response>
    /// <response code="404">Workspace não encontrado.</response>
    [HttpGet("{id:guid}/teams")]
    [ProducesResponseType(typeof(PagedResult<Team>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<Team>>> GetTeams(
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

        var result = await listWorkspaceTeams.ExecuteAsync(id, page, pageSize, cancellationToken);
        return FromResultOk(result);
    }
}
