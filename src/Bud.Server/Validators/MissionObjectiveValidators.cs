using Bud.Shared.Contracts;
using FluentValidation;

namespace Bud.Server.Validators;

public sealed class CreateMissionObjectiveValidator : AbstractValidator<CreateMissionObjectiveRequest>
{
    public CreateMissionObjectiveValidator()
    {
        RuleFor(x => x.MissionId)
            .NotEmpty().WithMessage("Missão é obrigatória.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Descrição deve ter no máximo 1000 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}

public sealed class UpdateMissionObjectiveValidator : AbstractValidator<UpdateMissionObjectiveRequest>
{
    public UpdateMissionObjectiveValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Descrição deve ter no máximo 1000 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
