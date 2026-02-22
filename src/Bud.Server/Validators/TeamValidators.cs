using Bud.Shared.Contracts;
using FluentValidation;

namespace Bud.Server.Validators;

public sealed class CreateTeamValidator : AbstractValidator<CreateTeamRequest>
{
    public CreateTeamValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres.");

        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace é obrigatório.");

        RuleFor(x => x.LeaderId)
            .NotEmpty().WithMessage("Líder é obrigatório.");
    }
}

public sealed class PatchTeamValidator : AbstractValidator<PatchTeamRequest>
{
    public PatchTeamValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres.");

        RuleFor(x => x.LeaderId)
            .NotEmpty().WithMessage("Líder é obrigatório.");
    }
}
