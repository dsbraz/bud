using Bud.Server.Authorization;
using Bud.Server.Application.Teams;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bud.Server.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.TenantSelected)]
[Route("api/teams")]
[Produces("application/json")]
public sealed class TeamsController(
    ITeamQueryUseCase teamQueryUseCase,
    ITeamCommandUseCase teamCommandUseCase,
    IValidator<CreateTeamRequest> createValidator,
    IValidator<UpdateTeamRequest> updateValidator) : ApiControllerBase
{
    /// <summary>
    /// Cria um time.
    /// </summary>
    /// <response code="201">Time criado com sucesso.</response>
    /// <response code="400">Payload inválido.</response>
    /// <response code="404">Workspace não encontrado.</response>
    /// <response code="403">Sem permissão para criar time.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(Team), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Team>> Create(CreateTeamRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await teamCommandUseCase.CreateAsync(User, request, cancellationToken);
        return FromResult(result, team => CreatedAtAction(nameof(GetById), new { id = team.Id }, team));
    }

    /// <summary>
    /// Atualiza um time.
    /// </summary>
    /// <response code="200">Time atualizado com sucesso.</response>
    /// <response code="400">Payload inválido.</response>
    /// <response code="404">Time não encontrado.</response>
    /// <response code="403">Sem permissão para atualizar time.</response>
    [HttpPut("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(Team), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Team>> Update(Guid id, UpdateTeamRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await teamCommandUseCase.UpdateAsync(User, id, request, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Exclui um time.
    /// </summary>
    /// <response code="204">Time removido com sucesso.</response>
    /// <response code="404">Time não encontrado.</response>
    /// <response code="409">Conflito de integridade ao remover time.</response>
    /// <response code="403">Sem permissão para excluir time.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await teamCommandUseCase.DeleteAsync(User, id, cancellationToken);
        return FromResult(result, NoContent);
    }

    /// <summary>
    /// Busca time por identificador.
    /// </summary>
    /// <response code="200">Time encontrado.</response>
    /// <response code="404">Time não encontrado.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Team), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Team>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await teamQueryUseCase.GetByIdAsync(id, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Lista times com paginação e filtros.
    /// </summary>
    /// <response code="200">Lista paginada retornada com sucesso.</response>
    /// <response code="400">Parâmetros inválidos.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Team>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<Team>>> GetAll(
        [FromQuery] Guid? workspaceId,
        [FromQuery] Guid? parentTeamId,
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

        var result = await teamQueryUseCase.GetAllAsync(workspaceId, parentTeamId, searchValidation.Value, page, pageSize, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Lista sub-times de um time.
    /// </summary>
    /// <response code="200">Lista paginada retornada com sucesso.</response>
    /// <response code="400">Parâmetros inválidos.</response>
    /// <response code="404">Time não encontrado.</response>
    [HttpGet("{id:guid}/subteams")]
    [ProducesResponseType(typeof(PagedResult<Team>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<Team>>> GetSubTeams(
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

        var result = await teamQueryUseCase.GetSubTeamsAsync(id, page, pageSize, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Lista colaboradores de um time.
    /// </summary>
    /// <response code="200">Lista paginada retornada com sucesso.</response>
    /// <response code="400">Parâmetros inválidos.</response>
    /// <response code="404">Time não encontrado.</response>
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

        var result = await teamQueryUseCase.GetCollaboratorsAsync(id, page, pageSize, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Lista resumo dos colaboradores vinculados ao time.
    /// </summary>
    /// <response code="200">Resumo retornado com sucesso.</response>
    /// <response code="404">Time não encontrado.</response>
    [HttpGet("{id:guid}/collaborators-summary")]
    [ProducesResponseType(typeof(List<CollaboratorSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<CollaboratorSummaryDto>>> GetCollaboratorSummaries(Guid id, CancellationToken cancellationToken)
    {
        var result = await teamQueryUseCase.GetCollaboratorSummariesAsync(id, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Atualiza vínculos de colaboradores do time.
    /// </summary>
    /// <response code="204">Vínculos atualizados com sucesso.</response>
    /// <response code="400">Payload inválido.</response>
    /// <response code="404">Time não encontrado.</response>
    /// <response code="403">Sem permissão para atualizar vínculos.</response>
    [HttpPut("{id:guid}/collaborators")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateCollaborators(Guid id, UpdateTeamCollaboratorsRequest request, CancellationToken cancellationToken)
    {
        var result = await teamCommandUseCase.UpdateCollaboratorsAsync(User, id, request, cancellationToken);
        return FromResult(result, NoContent);
    }

    /// <summary>
    /// Lista colaboradores disponíveis para vinculação ao time.
    /// </summary>
    /// <response code="200">Lista retornada com sucesso.</response>
    /// <response code="400">Parâmetros inválidos.</response>
    /// <response code="404">Time não encontrado.</response>
    [HttpGet("{id:guid}/available-collaborators")]
    [ProducesResponseType(typeof(List<CollaboratorSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<CollaboratorSummaryDto>>> GetAvailableCollaborators(
        Guid id,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var searchValidation = ValidateAndNormalizeSearch(search);
        if (searchValidation.Failure is not null)
        {
            return searchValidation.Failure;
        }

        var result = await teamQueryUseCase.GetAvailableCollaboratorsAsync(id, searchValidation.Value, cancellationToken);
        return FromResultOk(result);
    }
}
