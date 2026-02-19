using Bud.Server.Application.Auth;
using Bud.Shared.Contracts;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Bud.Server.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController(
    IAuthCommandUseCase authCommandUseCase,
    IAuthQueryUseCase authQueryUseCase,
    IValidator<AuthLoginRequest> loginValidator) : ApiControllerBase
{
    /// <summary>
    /// Realiza login por e-mail e retorna token JWT.
    /// </summary>
    /// <response code="200">Login realizado com sucesso.</response>
    /// <response code="400">Payload inválido.</response>
    /// <response code="404">Usuário não encontrado.</response>
    /// <response code="429">Limite de requisições excedido.</response>
    [HttpPost("login")]
    [EnableRateLimiting("auth-login")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(AuthLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthLoginResponse>> Login(AuthLoginRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblemFrom(validationResult);
        }

        var result = await authCommandUseCase.LoginAsync(request, cancellationToken);
        return FromResultOk(result);
    }

    /// <summary>
    /// Encerra a sessão do usuário no cliente.
    /// </summary>
    /// <response code="204">Logout concluído.</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        return NoContent();
    }

    /// <summary>
    /// Lista organizações disponíveis para o usuário autenticado.
    /// </summary>
    /// <response code="200">Lista retornada com sucesso.</response>
    /// <response code="400">Cabeçalho de e-mail ausente ou inválido.</response>
    [HttpGet("my-organizations")]
    [ProducesResponseType(typeof(List<OrganizationSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<OrganizationSummaryDto>>> GetMyOrganizations(
        [FromHeader(Name = "X-User-Email")] string email,
        CancellationToken cancellationToken)
    {
        var result = await authQueryUseCase.GetMyOrganizationsAsync(email, cancellationToken);
        return FromResultOk(result);
    }
}
