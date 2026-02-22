using Bud.Shared.Contracts;
using FluentValidation;

namespace Bud.Server.Validators;

public sealed class CreateMissionObjectiveValidator : AbstractValidator<CreateObjectiveRequest>
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

        RuleFor(x => x.Dimension)
            .MaximumLength(100).WithMessage("Dimensão deve ter no máximo 100 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Dimension));

    }
}

public sealed class PatchMissionObjectiveValidator : AbstractValidator<PatchObjectiveRequest>
{
    public PatchMissionObjectiveValidator()
    {
        RuleFor(x => x.Name.Value)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres.")
            .When(x => x.Name.HasValue);

        RuleFor(x => x.Description.Value)
            .MaximumLength(1000).WithMessage("Descrição deve ter no máximo 1000 caracteres.")
            .When(x => x.Description.HasValue && !string.IsNullOrEmpty(x.Description.Value));

        RuleFor(x => x.Dimension.Value)
            .MaximumLength(100).WithMessage("Dimensão deve ter no máximo 100 caracteres.")
            .When(x => x.Dimension.HasValue && !string.IsNullOrEmpty(x.Dimension.Value));

    }
}
