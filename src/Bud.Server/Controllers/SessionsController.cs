using Bud.Server.Application.UseCases.Sessions;
using Bud.Server.Authorization;
using Bud.Shared.Contracts;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Bud.Server.Domain.Model;

namespace Bud.Server.Controllers;

[ApiController]
[Route("api/sessions")]
[Produces("application/json")]
public sealed class SessionsController(
    CreateSession createSession,
    DeleteCurrentSession deleteCurrentSession,
    IValidator<CreateSessionRequest> loginValidator) : ApiControllerBase
{
    /// <summary>
    /// Realiza login por e-mail e retorna token JWT.
    /// </summary>
    /// <response code="200">Login realizado com sucesso.</response>
    /// <response code="400">Payload inválido.</response>
    /// <response code="404">Usuário não encontrado.</response>
    /// <response code="429">Limite de requisições excedido.</response>
    [HttpPost]
    [EnableRateLimiting("auth-login")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<SessionResponse>> Login(CreateSessionRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await createSession.ExecuteAsync(request, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Encerra a sessão do usuário no cliente.
    /// </summary>
    /// <response code="204">Logout concluído.</response>
    [Authorize(Policy = AuthorizationPolicies.TenantSelected)]
    [HttpDelete("current")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var result = await deleteCurrentSession.ExecuteAsync(cancellationToken);
        return FromResult(result, NoContent);
    }
}
