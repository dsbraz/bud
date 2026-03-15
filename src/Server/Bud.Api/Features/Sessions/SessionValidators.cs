using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Bud.Api.Features.Sessions;

public sealed class CreateSessionValidator : AbstractValidator<CreateSessionRequest>
{
    private const string AdminAlias = "admin";
    private static readonly EmailAddressAttribute EmailValidator = new();

    public CreateSessionValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .Must(email => IsAdminAlias(email) || EmailValidator.IsValid(email))
            .WithMessage("Informe um e-mail válido ou use o login admin.");
    }

    private static bool IsAdminAlias(string? email)
    {
        var normalized = email?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        return string.Equals(normalized, AdminAlias, StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith($"{AdminAlias}@", StringComparison.OrdinalIgnoreCase);
    }
}
