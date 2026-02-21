using Bud.Server.Application.ObjectiveDimensions;
using Bud.Server.Authorization;
using Bud.Shared.Contracts;
using Bud.Server.Domain.Model;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bud.Server.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.TenantSelected)]
[Route("api/objective-dimensions")]
[Produces("application/json")]
public sealed class ObjectiveDimensionsController(
    RegisterStrategicDimension registerStrategicDimension,
    RenameStrategicDimension renameStrategicDimension,
    RemoveStrategicDimension removeStrategicDimension,
    ViewStrategicDimensionDetails viewStrategicDimensionDetails,
    ListStrategicDimensions listStrategicDimensions,
    IValidator<CreateObjectiveDimensionRequest> createValidator,
    IValidator<UpdateObjectiveDimensionRequest> updateValidator) : ApiControllerBase
{
    /// <summary>
    /// Cria uma nova dimensão de objetivo.
    /// </summary>
    /// <response code="201">Dimensão criada com sucesso.</response>
    /// <response code="400">Payload inválido.</response>
    /// <response code="403">Sem permissão para criar dimensão.</response>
    /// <response code="409">Já existe dimensão com o mesmo nome.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ObjectiveDimension), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ObjectiveDimension>> Create(CreateObjectiveDimensionRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await registerStrategicDimension.ExecuteAsync(User, request, cancellationToken);
        return FromResult(result, dimension => CreatedAtAction(nameof(GetById), new { id = dimension.Id }, dimension));
    }

    /// <summary>
    /// Atualiza uma dimensão de objetivo.
    /// </summary>
    /// <response code="200">Dimensão atualizada com sucesso.</response>
    /// <response code="400">Payload inválido.</response>
    /// <response code="403">Sem permissão para atualizar dimensão.</response>
    /// <response code="404">Dimensão não encontrada.</response>
    /// <response code="409">Já existe dimensão com o mesmo nome.</response>
    [HttpPut("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ObjectiveDimension), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ObjectiveDimension>> Update(
        Guid id,
        UpdateObjectiveDimensionRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await renameStrategicDimension.ExecuteAsync(User, id, request, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Remove uma dimensão de objetivo.
    /// </summary>
    /// <response code="204">Dimensão removida com sucesso.</response>
    /// <response code="403">Sem permissão para excluir dimensão.</response>
    /// <response code="404">Dimensão não encontrada.</response>
    /// <response code="409">Dimensão em uso por objetivos.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await removeStrategicDimension.ExecuteAsync(User, id, cancellationToken);
        return FromResult(result, NoContent);
    }

    /// <summary>
    /// Busca uma dimensão de objetivo por identificador.
    /// </summary>
    /// <response code="200">Dimensão encontrada.</response>
    /// <response code="404">Dimensão não encontrada.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ObjectiveDimension), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ObjectiveDimension>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await viewStrategicDimensionDetails.ExecuteAsync(id, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Lista dimensões de objetivo com busca e paginação.
    /// </summary>
    /// <response code="200">Lista paginada retornada com sucesso.</response>
    /// <response code="400">Parâmetros inválidos.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ObjectiveDimension>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ObjectiveDimension>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var listValidation = ValidateListParameters(search, page, pageSize);
        if (listValidation.Failure is not null)
        {
            return listValidation.Failure;
        }

        var result = await listStrategicDimensions.ExecuteAsync(listValidation.Search, page, pageSize, cancellationToken);
        return FromResultOk(result);
    }
}
