using System.ComponentModel.DataAnnotations;
using Bud.Shared.Contracts;
using FluentValidation;

namespace Bud.Server.Validators;

public sealed class CreateSessionRequestValidator : AbstractValidator<CreateSessionRequest>
{
    private const string AdminAlias = "admin";
    private static readonly EmailAddressAttribute EmailValidator = new();

    public CreateSessionRequestValidator()
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
