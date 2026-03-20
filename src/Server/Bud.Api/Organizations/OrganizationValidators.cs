using Bud.Shared.Contracts;
using FluentValidation;

namespace Bud.Api.Organizations;

public sealed class CreateOrganizationValidator : AbstractValidator<CreateOrganizationRequest>
{
    public CreateOrganizationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome da organização é obrigatório.")
            .MaximumLength(255).WithMessage("O nome não pode exceder 255 caracteres.");

        RuleFor(x => x.Plan)
            .MaximumLength(255).WithMessage("O plano não pode exceder 255 caracteres.")
            .When(x => x.Plan is not null);

        RuleFor(x => x.IconUrl)
            .MaximumLength(255).WithMessage("A URL do ícone não pode exceder 255 caracteres.")
            .When(x => x.IconUrl is not null);
    }
}

public sealed class PatchOrganizationValidator : AbstractValidator<PatchOrganizationRequest>
{
    public PatchOrganizationValidator()
    {
        RuleFor(x => x.Name.Value)
            .NotEmpty().WithMessage("O nome da organização é obrigatório.")
            .MaximumLength(255).WithMessage("O nome não pode exceder 255 caracteres.")
            .When(x => x.Name.HasValue);

        RuleFor(x => x.Plan.Value)
            .MaximumLength(255).WithMessage("O plano não pode exceder 255 caracteres.")
            .When(x => x.Plan.HasValue && x.Plan.Value is not null);

        RuleFor(x => x.IconUrl.Value)
            .MaximumLength(255).WithMessage("A URL do ícone não pode exceder 255 caracteres.")
            .When(x => x.IconUrl.HasValue && x.IconUrl.Value is not null);
    }
}
