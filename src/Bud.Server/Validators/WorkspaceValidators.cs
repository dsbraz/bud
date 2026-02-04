using Bud.Shared.Contracts;
using FluentValidation;

namespace Bud.Server.Validators;

public sealed class CreateWorkspaceValidator : AbstractValidator<CreateWorkspaceRequest>
{
    public CreateWorkspaceValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres.");

        RuleFor(x => x.OrganizationId)
            .NotEmpty().WithMessage("Organização é obrigatória.");

        RuleFor(x => x.Visibility)
            .NotNull().WithMessage("Visibilidade é obrigatória.")
            .IsInEnum().WithMessage("Visibilidade deve ser um valor válido (Pública ou Privada).");
    }
}

public sealed class UpdateWorkspaceValidator : AbstractValidator<UpdateWorkspaceRequest>
{
    public UpdateWorkspaceValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres.");

        RuleFor(x => x.Visibility)
            .IsInEnum().WithMessage("Visibilidade deve ser um valor válido (Pública ou Privada).");
    }
}
