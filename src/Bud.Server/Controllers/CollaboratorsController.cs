using Bud.Server.Application.Collaborators;
using Bud.Server.Authorization;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bud.Server.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.TenantSelected)]
[Route("api/collaborators")]
[Produces("application/json")]
public sealed class CollaboratorsController(
    ICollaboratorQueryUseCase collaboratorQueryUseCase,
    ICollaboratorCommandUseCase collaboratorCommandUseCase,
    IValidator<CreateCollaboratorRequest> createValidator,
    IValidator<UpdateCollaboratorRequest> updateValidator) : ApiControllerBase
{
    /// <summary>
    /// Cria um colaborador.
    /// </summary>
    /// <response code="201">Colaborador criado com sucesso.</response>
    /// <response code="400">Payload inválido.</response>
    /// <response code="404">Organização ou time não encontrado.</response>
    /// <response code="403">Sem permissão para criar colaborador.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(Collaborator), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Collaborator>> Create(CreateCollaboratorRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await collaboratorCommandUseCase.CreateAsync(User, request, cancellationToken);
        return FromResult(result, collaborator => CreatedAtAction(nameof(GetById), new { id = collaborator.Id }, collaborator));
    }

    /// <summary>
    /// Atualiza um colaborador.
    /// </summary>
    /// <response code="200">Colaborador atualizado com sucesso.</response>
    /// <response code="400">Payload inválido.</response>
    /// <response code="404">Colaborador não encontrado.</response>
    /// <response code="403">Sem permissão para atualizar colaborador.</response>
    [HttpPut("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(Collaborator), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Collaborator>> Update(Guid id, UpdateCollaboratorRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await collaboratorCommandUseCase.UpdateAsync(User, id, request, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Exclui um colaborador.
    /// </summary>
    /// <response code="204">Colaborador removido com sucesso.</response>
    /// <response code="404">Colaborador não encontrado.</response>
    /// <response code="409">Conflito de integridade ao remover colaborador.</response>
    /// <response code="403">Sem permissão para excluir colaborador.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await collaboratorCommandUseCase.DeleteAsync(User, id, cancellationToken);
        return FromResult(result, NoContent);
    }

    /// <summary>
    /// Busca colaborador por identificador.
    /// </summary>
    /// <response code="200">Colaborador encontrado.</response>
    /// <response code="404">Colaborador não encontrado.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Collaborator), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Collaborator>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await collaboratorQueryUseCase.GetByIdAsync(id, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Lista colaboradores líderes.
    /// </summary>
    /// <response code="200">Lista retornada com sucesso.</response>
    [HttpGet("leaders")]
    [ProducesResponseType(typeof(List<LeaderCollaboratorResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<LeaderCollaboratorResponse>>> GetLeaders(
        [FromQuery] Guid? organizationId,
        CancellationToken cancellationToken)
    {
        var result = await collaboratorQueryUseCase.GetLeadersAsync(organizationId, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Lista colaboradores com paginação e filtros.
    /// </summary>
    /// <response code="200">Lista paginada retornada com sucesso.</response>
    /// <response code="400">Parâmetros inválidos.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Collaborator>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<Collaborator>>> GetAll(
        [FromQuery] Guid? teamId,
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

        var result = await collaboratorQueryUseCase.GetAllAsync(teamId, searchValidation.Value, page, pageSize, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Lista subordinados (liderados) em hierarquia recursiva.
    /// </summary>
    /// <response code="200">Hierarquia retornada com sucesso.</response>
    /// <response code="404">Colaborador não encontrado.</response>
    [HttpGet("{id:guid}/subordinates")]
    [ProducesResponseType(typeof(List<CollaboratorHierarchyNodeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<CollaboratorHierarchyNodeDto>>> GetSubordinates(Guid id, CancellationToken cancellationToken)
    {
        var result = await collaboratorQueryUseCase.GetSubordinatesAsync(id, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Lista times associados ao colaborador.
    /// </summary>
    /// <response code="200">Lista retornada com sucesso.</response>
    /// <response code="404">Colaborador não encontrado.</response>
    [HttpGet("{id:guid}/teams")]
    [ProducesResponseType(typeof(List<TeamSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<TeamSummaryDto>>> GetTeams(Guid id, CancellationToken cancellationToken)
    {
        var result = await collaboratorQueryUseCase.GetTeamsAsync(id, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Atualiza times associados ao colaborador.
    /// </summary>
    /// <response code="204">Vínculos atualizados com sucesso.</response>
    /// <response code="400">Payload inválido.</response>
    /// <response code="404">Colaborador não encontrado.</response>
    /// <response code="403">Sem permissão para atualizar vínculos.</response>
    [HttpPut("{id:guid}/teams")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateTeams(Guid id, UpdateCollaboratorTeamsRequest request, CancellationToken cancellationToken)
    {
        var result = await collaboratorCommandUseCase.UpdateTeamsAsync(User, id, request, cancellationToken);
        return FromResult(result, NoContent);
    }

    /// <summary>
    /// Lista times disponíveis para vínculo com o colaborador.
    /// </summary>
    /// <response code="200">Lista retornada com sucesso.</response>
    /// <response code="400">Parâmetros inválidos.</response>
    /// <response code="404">Colaborador não encontrado.</response>
    [HttpGet("{id:guid}/available-teams")]
    [ProducesResponseType(typeof(List<TeamSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<TeamSummaryDto>>> GetAvailableTeams(
        Guid id,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var searchValidation = ValidateAndNormalizeSearch(search);
        if (searchValidation.Failure is not null)
        {
            return searchValidation.Failure;
        }

        var result = await collaboratorQueryUseCase.GetAvailableTeamsAsync(id, searchValidation.Value, cancellationToken);
        return FromResultOk(result);
    }
}
