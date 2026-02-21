using Bud.Server.Application.MissionTemplates;
using Bud.Server.Authorization;
using Bud.Shared.Contracts;
using Bud.Server.Domain.Model;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bud.Server.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.TenantSelected)]
[Route("api/mission-templates")]
[Produces("application/json")]
public sealed class MissionTemplatesController(
    CreateStrategicMissionTemplate createStrategicMissionTemplate,
    ReviseStrategicMissionTemplate reviseStrategicMissionTemplate,
    RemoveStrategicMissionTemplate removeStrategicMissionTemplate,
    ViewStrategicMissionTemplate viewStrategicMissionTemplate,
    ListMissionTemplates listMissionTemplates,
    IValidator<CreateMissionTemplateRequest> createValidator,
    IValidator<UpdateMissionTemplateRequest> updateValidator) : ApiControllerBase
{
    /// <summary>
    /// Cria um novo template de missão.
    /// </summary>
    /// <response code="201">Template criado com sucesso.</response>
    /// <response code="400">Payload inválido ou erro de validação.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MissionTemplate), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MissionTemplate>> Create(CreateMissionTemplateRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await createStrategicMissionTemplate.ExecuteAsync(User, request, cancellationToken);
        return FromResult(result, template => CreatedAtAction(nameof(GetById), new { id = template.Id }, template));
    }

    /// <summary>
    /// Atualiza um template de missão existente.
    /// </summary>
    /// <response code="200">Template atualizado com sucesso.</response>
    /// <response code="400">Payload inválido ou erro de validação.</response>
    /// <response code="404">Template não encontrado.</response>
    [HttpPut("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MissionTemplate), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MissionTemplate>> Update(Guid id, UpdateMissionTemplateRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await reviseStrategicMissionTemplate.ExecuteAsync(User, id, request, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Remove um template de missão pelo identificador.
    /// </summary>
    /// <response code="204">Template removido com sucesso.</response>
    /// <response code="404">Template não encontrado.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await removeStrategicMissionTemplate.ExecuteAsync(User, id, cancellationToken);
        return FromResult(result, NoContent);
    }

    /// <summary>
    /// Busca um template de missão pelo identificador.
    /// </summary>
    /// <response code="200">Template encontrado.</response>
    /// <response code="404">Template não encontrado.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MissionTemplate), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MissionTemplate>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await viewStrategicMissionTemplate.ExecuteAsync(id, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Lista templates de missão com busca e paginação.
    /// </summary>
    /// <response code="200">Lista paginada retornada com sucesso.</response>
    /// <response code="400">Parâmetros de filtro/paginação inválidos.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<MissionTemplate>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<MissionTemplate>>> GetAll(
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

        var result = await listMissionTemplates.ExecuteAsync(searchValidation.Value, page, pageSize, cancellationToken);
        return FromResultOk(result);
    }
}
