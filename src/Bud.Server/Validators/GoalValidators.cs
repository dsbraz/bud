using Bud.Shared.Contracts.Requests;
using FluentValidation;

namespace Bud.Server.Validators;

public sealed class CreateGoalValidator : AbstractValidator<CreateGoalRequest>
{
    public CreateGoalValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Descrição deve ter no máximo 1000 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Dimension)
            .MaximumLength(100).WithMessage("Dimensão deve ter no máximo 100 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Dimension));

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Data de início é obrigatória.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("Data de término é obrigatória.")
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("Data de término deve ser igual ou posterior à data de início.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status inválido.");
    }
}

public sealed class PatchGoalValidator : AbstractValidator<PatchGoalRequest>
{
    public PatchGoalValidator()
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

        RuleFor(x => x.StartDate.Value)
            .NotEmpty().WithMessage("Data de início é obrigatória.")
            .When(x => x.StartDate.HasValue);

        RuleFor(x => x.EndDate.Value)
            .NotEmpty().WithMessage("Data de término é obrigatória.")
            .When(x => x.EndDate.HasValue);

        RuleFor(x => x.Status.Value)
            .IsInEnum().WithMessage("Status inválido.")
            .When(x => x.Status.HasValue);

        RuleFor(x => x)
            .Must(x =>
            {
                if (x.StartDate.HasValue && x.EndDate.HasValue)
                {
                    return x.EndDate.Value >= x.StartDate.Value;
                }

                return true;
            })
            .WithMessage("Data de término deve ser igual ou posterior à data de início.");
    }
}
