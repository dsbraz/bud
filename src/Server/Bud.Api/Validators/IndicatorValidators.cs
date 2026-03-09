using Bud.Shared.Contracts;
using Bud.Shared.Contracts.Requests;
using FluentValidation;

namespace Bud.Api.Validators;

public sealed class CreateIndicatorValidator : AbstractValidator<CreateIndicatorRequest>
{
    public CreateIndicatorValidator()
    {
        RuleFor(x => x.GoalId)
            .NotEmpty().WithMessage("Meta é obrigatória.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Tipo inválido.");

        When(x => x.Type == IndicatorType.Qualitative, () =>
        {
            RuleFor(x => x.TargetText)
                .MaximumLength(1000).WithMessage("Texto alvo deve ter no máximo 1000 caracteres.");
        });

        When(x => x.Type == IndicatorType.Quantitative, () =>
        {
            RuleFor(x => x.QuantitativeType)
                .NotNull().WithMessage("Tipo quantitativo é obrigatório para indicadores quantitativos.")
                .IsInEnum().WithMessage("Tipo quantitativo inválido.");

            RuleFor(x => x.Unit)
                .NotNull().WithMessage("Unidade é obrigatória para indicadores quantitativos.")
                .IsInEnum().WithMessage("Unidade inválida.");

            When(x => x.QuantitativeType == QuantitativeIndicatorType.KeepAbove, () =>
            {
                RuleFor(x => x.MinValue)
                    .NotNull().WithMessage("Valor mínimo é obrigatório para indicadores KeepAbove.")
                    .GreaterThanOrEqualTo(0).WithMessage("Valor mínimo deve ser maior ou igual a 0.");
            });

            When(x => x.QuantitativeType == QuantitativeIndicatorType.KeepBelow, () =>
            {
                RuleFor(x => x.MaxValue)
                    .NotNull().WithMessage("Valor máximo é obrigatório para indicadores KeepBelow.")
                    .GreaterThanOrEqualTo(0).WithMessage("Valor máximo deve ser maior ou igual a 0.");
            });

            When(x => x.QuantitativeType == QuantitativeIndicatorType.KeepBetween, () =>
            {
                RuleFor(x => x.MinValue)
                    .NotNull().WithMessage("Valor mínimo é obrigatório para indicadores KeepBetween.")
                    .GreaterThanOrEqualTo(0).WithMessage("Valor mínimo deve ser maior ou igual a 0.");

                RuleFor(x => x.MaxValue)
                    .NotNull().WithMessage("Valor máximo é obrigatório para indicadores KeepBetween.")
                    .GreaterThanOrEqualTo(0).WithMessage("Valor máximo deve ser maior ou igual a 0.")
                    .GreaterThan(x => x.MinValue ?? 0).WithMessage("Valor máximo deve ser maior que o valor mínimo.");
            });

            When(x => x.QuantitativeType == QuantitativeIndicatorType.Achieve, () =>
            {
                RuleFor(x => x.MaxValue)
                    .NotNull().WithMessage("Valor máximo é obrigatório para indicadores Achieve.")
                    .GreaterThanOrEqualTo(0).WithMessage("Valor máximo deve ser maior ou igual a 0.");
            });

            When(x => x.QuantitativeType == QuantitativeIndicatorType.Reduce, () =>
            {
                RuleFor(x => x.MaxValue)
                    .NotNull().WithMessage("Valor máximo é obrigatório para indicadores Reduce.")
                    .GreaterThanOrEqualTo(0).WithMessage("Valor máximo deve ser maior ou igual a 0.");
            });
        });
    }
}

public sealed class PatchIndicatorValidator : AbstractValidator<PatchIndicatorRequest>
{
    public PatchIndicatorValidator()
    {
        RuleFor(x => x.Name.Value)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres.")
            .When(x => x.Name.HasValue);

        RuleFor(x => x.Type.Value)
            .IsInEnum().WithMessage("Tipo inválido.")
            .When(x => x.Type.HasValue);

        RuleFor(x => x.TargetText.Value)
            .MaximumLength(1000).WithMessage("Texto alvo deve ter no máximo 1000 caracteres.")
            .When(x => x.TargetText.HasValue && !string.IsNullOrEmpty(x.TargetText.Value));

        RuleFor(x => x.QuantitativeType.Value)
            .IsInEnum().WithMessage("Tipo quantitativo inválido.")
            .When(x => x.QuantitativeType.HasValue && x.QuantitativeType.Value.HasValue);

        RuleFor(x => x.Unit.Value)
            .IsInEnum().WithMessage("Unidade inválida.")
            .When(x => x.Unit.HasValue && x.Unit.Value.HasValue);

        RuleFor(x => x.MinValue.Value)
            .GreaterThanOrEqualTo(0).WithMessage("Valor mínimo deve ser maior ou igual a 0.")
            .When(x => x.MinValue.HasValue && x.MinValue.Value.HasValue);

        RuleFor(x => x.MaxValue.Value)
            .GreaterThanOrEqualTo(0).WithMessage("Valor máximo deve ser maior ou igual a 0.")
            .When(x => x.MaxValue.HasValue && x.MaxValue.Value.HasValue);
    }
}
