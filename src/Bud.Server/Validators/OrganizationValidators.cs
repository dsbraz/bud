using Bud.Shared.Contracts;
using FluentValidation;

namespace Bud.Server.Validators;

public sealed class CreateOrganizationValidator : AbstractValidator<CreateOrganizationRequest>
{
    public CreateOrganizationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.OwnerId)
            .NotEmpty().WithMessage("Um líder deve ser selecionado como proprietário da organização.");

        RuleFor(x => x.UserEmail)
            .NotEmpty().WithMessage("Email do usuário é obrigatório.");
    }
}

public sealed class UpdateOrganizationValidator : AbstractValidator<UpdateOrganizationRequest>
{
    public UpdateOrganizationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");
    }
}
