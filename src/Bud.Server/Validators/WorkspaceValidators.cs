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
    }
}

public sealed class PatchWorkspaceValidator : AbstractValidator<PatchWorkspaceRequest>
{
    public PatchWorkspaceValidator()
    {
        RuleFor(x => x.Name.Value)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres.")
            .When(x => x.Name.HasValue);
    }
}
