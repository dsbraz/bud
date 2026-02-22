using Bud.Shared.Contracts;
using FluentValidation;

namespace Bud.Server.Validators;

public sealed class CreateOrganizationValidator : AbstractValidator<CreateOrganizationRequest>
{
    public CreateOrganizationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O domínio é obrigatório.")
            .MaximumLength(200).WithMessage("O domínio não pode exceder 200 caracteres.")
            .Matches(@"^([a-zA-Z0-9]([a-zA-Z0-9-]*[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}$")
            .WithMessage("O nome deve ser um domínio válido (ex: empresa.com.br).");

        RuleFor(x => x.OwnerId)
            .NotEmpty().WithMessage("Um líder deve ser selecionado como proprietário da organização.");
    }
}

public sealed class PatchOrganizationValidator : AbstractValidator<PatchOrganizationRequest>
{
    public PatchOrganizationValidator()
    {
        RuleFor(x => x.Name.Value)
            .NotEmpty().WithMessage("O domínio é obrigatório.")
            .MaximumLength(200).WithMessage("O domínio não pode exceder 200 caracteres.")
            .Matches(@"^([a-zA-Z0-9]([a-zA-Z0-9-]*[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}$")
            .WithMessage("O nome deve ser um domínio válido (ex: empresa.com.br).")
            .When(x => x.Name.HasValue);

        RuleFor(x => x.OwnerId.Value)
            .NotEmpty().WithMessage("Um líder deve ser selecionado como proprietário da organização.")
            .When(x => x.OwnerId.HasValue && x.OwnerId.Value != Guid.Empty);
    }
}
