using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.Model;

namespace Bud.Server.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected sealed record LoadEntityAuthorizationResult<TEntity>(TEntity? Entity, ActionResult? Failure)
        where TEntity : class;
    protected sealed record GuidCsvParseResult(List<Guid>? Values, ActionResult? Failure);
    protected sealed record SearchValidationResult(string? Value, ActionResult? Failure);
    protected sealed record ListParametersValidationResult(string? Search, ActionResult? Failure);

    protected ActionResult ValidationProblemFrom(ValidationResult validationResult)
    {
        return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
    }

    protected ObjectResult ForbiddenProblem(string detail)
    {
        return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
        {
            Title = "Acesso negado",
            Detail = detail
        });
    }

    protected async Task<ActionResult?> EnsureAuthorizedAsync(
        IAuthorizationService authorizationService,
        object resource,
        string policyName,
        string forbiddenDetail)
    {
        var authResult = await authorizationService.AuthorizeAsync(User, resource, policyName);
        if (authResult.Succeeded)
        {
            return null;
        }

        return ForbiddenProblem(forbiddenDetail);
    }

    protected async Task<LoadEntityAuthorizationResult<TEntity>> LoadEntityAndEnsureAuthorizedAsync<TEntity>(
        Func<CancellationToken, Task<TEntity?>> loadEntityAsync,
        Func<TEntity, object> resourceFactory,
        IAuthorizationService authorizationService,
        string policyName,
        string notFoundDetail,
        string forbiddenDetail,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        var entity = await loadEntityAsync(cancellationToken);
        if (entity is null)
        {
            return new LoadEntityAuthorizationResult<TEntity>(
                null,
                NotFound(new ProblemDetails { Detail = notFoundDetail }));
        }

        var authorizationFailure = await EnsureAuthorizedAsync(
            authorizationService,
            resourceFactory(entity),
            policyName,
            forbiddenDetail);

        return authorizationFailure is null
            ? new LoadEntityAuthorizationResult<TEntity>(entity, null)
            : new LoadEntityAuthorizationResult<TEntity>(null, authorizationFailure);
    }

    protected ActionResult FromResult(Result result, Func<ActionResult> onSuccess)
    {
        if (result.IsSuccess)
        {
            return onSuccess();
        }

        return result.ErrorType switch
        {
            ErrorType.NotFound => NotFound(new ProblemDetails { Detail = result.Error }),
            ErrorType.Conflict => Conflict(new ProblemDetails { Detail = result.Error }),
            ErrorType.Forbidden => ForbiddenProblem(result.Error ?? "Você não tem permissão para realizar esta ação."),
            _ => BadRequest(new ProblemDetails { Detail = result.Error })
        };
    }

    protected ActionResult<T> FromResult<T>(Result<T> result, Func<T, ActionResult<T>> onSuccess)
    {
        if (result.IsSuccess)
        {
            return onSuccess(result.Value!);
        }

        return result.ErrorType switch
        {
            ErrorType.NotFound => NotFound(new ProblemDetails { Detail = result.Error }),
            ErrorType.Conflict => Conflict(new ProblemDetails { Detail = result.Error }),
            ErrorType.Forbidden => ForbiddenProblem(result.Error ?? "Você não tem permissão para realizar esta ação."),
            _ => BadRequest(new ProblemDetails { Detail = result.Error })
        };
    }

    protected ActionResult<T> FromResultOk<T>(Result<T> result)
    {
        return FromResult(result, value => Ok(value));
    }

    protected ActionResult<TResult> FromResultOk<T, TResult>(Result<T> result, Func<T, TResult> mapper)
    {
        if (result.IsSuccess)
        {
            return Ok(mapper(result.Value!));
        }

        return result.ErrorType switch
        {
            ErrorType.NotFound => NotFound(new ProblemDetails { Detail = result.Error }),
            ErrorType.Conflict => Conflict(new ProblemDetails { Detail = result.Error }),
            ErrorType.Forbidden => ForbiddenProblem(result.Error ?? "Você não tem permissão para realizar esta ação."),
            _ => BadRequest(new ProblemDetails { Detail = result.Error })
        };
    }

    protected ActionResult? ValidatePagination(int page, int pageSize, int maxPageSize = 100)
    {
        if (page < 1)
        {
            return BadRequest(new ProblemDetails
            {
                Detail = "O parâmetro 'page' deve ser maior ou igual a 1."
            });
        }

        if (pageSize < 1 || pageSize > maxPageSize)
        {
            return BadRequest(new ProblemDetails
            {
                Detail = $"O parâmetro 'pageSize' deve estar entre 1 e {maxPageSize}."
            });
        }

        return null;
    }

    protected GuidCsvParseResult ParseGuidCsv(string? csv, string parameterName)
    {
        var values = new List<Guid>();
        foreach (var part in (csv ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Guid.TryParse(part, out var value))
            {
                return new GuidCsvParseResult(
                    null,
                    BadRequest(new ProblemDetails
                    {
                        Detail = $"O parâmetro '{parameterName}' contém valores inválidos. Informe GUIDs separados por vírgula."
                    }));
            }

            values.Add(value);
        }

        return new GuidCsvParseResult(values, null);
    }

    protected SearchValidationResult ValidateAndNormalizeSearch(string? search, int maxLength = 200, string parameterName = "search")
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return new SearchValidationResult(null, null);
        }

        var normalized = search.Trim();
        if (normalized.Length > maxLength)
        {
            return new SearchValidationResult(
                null,
                BadRequest(new ProblemDetails
                {
                    Detail = $"O parâmetro '{parameterName}' deve ter no máximo {maxLength} caracteres."
                }));
        }

        return new SearchValidationResult(normalized, null);
    }

    protected ListParametersValidationResult ValidateListParameters(
        string? search,
        int page,
        int pageSize,
        int maxPageSize = 100,
        int maxSearchLength = 200)
    {
        var searchValidation = ValidateAndNormalizeSearch(search, maxSearchLength);
        if (searchValidation.Failure is not null)
        {
            return new ListParametersValidationResult(null, searchValidation.Failure);
        }

        var paginationFailure = ValidatePagination(page, pageSize, maxPageSize);
        if (paginationFailure is not null)
        {
            return new ListParametersValidationResult(null, paginationFailure);
        }

        return new ListParametersValidationResult(searchValidation.Value, null);
    }
}
