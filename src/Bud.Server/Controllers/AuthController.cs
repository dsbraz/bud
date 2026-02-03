using Bud.Server.Services;
using Bud.Shared.Contracts;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Bud.Server.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService, IValidator<AuthLoginRequest> loginValidator) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuthLoginResponse>> Login(AuthLoginRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(
                validationResult.ToDictionary()));
        }

        var result = await authService.LoginAsync(request, cancellationToken);
        if (result.IsFailure)
        {
            return result.ErrorType == ServiceErrorType.NotFound
                ? NotFound(new ProblemDetails { Detail = result.Error })
                : BadRequest(new ProblemDetails { Detail = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        return NoContent();
    }

    [HttpGet("my-organizations")]
    [ProducesResponseType(typeof(List<OrganizationSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<OrganizationSummaryDto>>> GetMyOrganizations(
        [FromHeader(Name = "X-User-Email")] string email,
        CancellationToken cancellationToken)
    {
        var result = await authService.GetMyOrganizationsAsync(email, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails { Detail = result.Error });
        }

        return Ok(result.Value);
    }
}
