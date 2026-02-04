using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentValidation;

namespace Bud.Server.Validators;

public sealed class CreateMissionValidator : AbstractValidator<CreateMissionRequest>
{
    public CreateMissionValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Descrição deve ter no máximo 1000 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Data de início é obrigatória.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("Data de término é obrigatória.")
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("Data de término deve ser igual ou posterior à data de início.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status inválido.");

        RuleFor(x => x.ScopeType)
            .IsInEnum().WithMessage("Tipo de escopo inválido.");

        RuleFor(x => x.ScopeId)
            .NotEmpty().WithMessage("Escopo é obrigatório.");
    }
}

public sealed class UpdateMissionValidator : AbstractValidator<UpdateMissionRequest>
{
    public UpdateMissionValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Data de início é obrigatória.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("Data de término é obrigatória.")
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("Data de término deve ser igual ou posterior à data de início.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status inválido.");
    }
}

public sealed class CreateMissionMetricValidator : AbstractValidator<CreateMissionMetricRequest>
{
    public CreateMissionMetricValidator()
    {
        RuleFor(x => x.MissionId)
            .NotEmpty().WithMessage("Missão é obrigatória.");

        ApplyMetricRules();
    }

    private void ApplyMetricRules()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Tipo inválido.");

        When(x => x.Type == MetricType.Qualitative, () =>
        {
            RuleFor(x => x.TargetText)
                .NotEmpty().WithMessage("Texto alvo é obrigatório para métricas qualitativas.")
                .MaximumLength(1000).WithMessage("Texto alvo deve ter no máximo 1000 caracteres.");
        });

        When(x => x.Type == MetricType.Quantitative, () =>
        {
            RuleFor(x => x.QuantitativeType)
                .NotNull().WithMessage("Tipo quantitativo é obrigatório para métricas quantitativas.")
                .IsInEnum().WithMessage("Tipo quantitativo inválido.");

            RuleFor(x => x.Unit)
                .NotNull().WithMessage("Unidade é obrigatória para métricas quantitativas.")
                .IsInEnum().WithMessage("Unidade inválida.");

            // KeepAbove: requires MinValue
            When(x => x.QuantitativeType == QuantitativeMetricType.KeepAbove, () =>
            {
                RuleFor(x => x.MinValue)
                    .NotNull().WithMessage("Valor mínimo é obrigatório para métricas KeepAbove.")
                    .GreaterThanOrEqualTo(0).WithMessage("Valor mínimo deve ser maior ou igual a 0.");
            });

            // KeepBelow: requires MaxValue
            When(x => x.QuantitativeType == QuantitativeMetricType.KeepBelow, () =>
            {
                RuleFor(x => x.MaxValue)
                    .NotNull().WithMessage("Valor máximo é obrigatório para métricas KeepBelow.")
                    .GreaterThanOrEqualTo(0).WithMessage("Valor máximo deve ser maior ou igual a 0.");
            });

            // KeepBetween: requires both MinValue and MaxValue, with MinValue < MaxValue
            When(x => x.QuantitativeType == QuantitativeMetricType.KeepBetween, () =>
            {
                RuleFor(x => x.MinValue)
                    .NotNull().WithMessage("Valor mínimo é obrigatório para métricas KeepBetween.")
                    .GreaterThanOrEqualTo(0).WithMessage("Valor mínimo deve ser maior ou igual a 0.");

                RuleFor(x => x.MaxValue)
                    .NotNull().WithMessage("Valor máximo é obrigatório para métricas KeepBetween.")
                    .GreaterThanOrEqualTo(0).WithMessage("Valor máximo deve ser maior ou igual a 0.")
                    .GreaterThan(x => x.MinValue ?? 0).WithMessage("Valor máximo deve ser maior que o valor mínimo.");
            });

            // Achieve: requires MaxValue (target to achieve)
            When(x => x.QuantitativeType == QuantitativeMetricType.Achieve, () =>
            {
                RuleFor(x => x.MaxValue)
                    .NotNull().WithMessage("Valor máximo é obrigatório para métricas Achieve.")
                    .GreaterThanOrEqualTo(0).WithMessage("Valor máximo deve ser maior ou igual a 0.");
            });

            // Reduce: requires MaxValue (target to reduce to)
            When(x => x.QuantitativeType == QuantitativeMetricType.Reduce, () =>
            {
                RuleFor(x => x.MaxValue)
                    .NotNull().WithMessage("Valor máximo é obrigatório para métricas Reduce.")
                    .GreaterThanOrEqualTo(0).WithMessage("Valor máximo deve ser maior ou igual a 0.");
            });
        });
    }
}

public sealed class UpdateMissionMetricValidator : AbstractValidator<UpdateMissionMetricRequest>
{
    public UpdateMissionMetricValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Tipo inválido.");

        When(x => x.Type == MetricType.Qualitative, () =>
        {
            RuleFor(x => x.TargetText)
                .NotEmpty().WithMessage("Texto alvo é obrigatório para métricas qualitativas.")
                .MaximumLength(1000).WithMessage("Texto alvo deve ter no máximo 1000 caracteres.");
        });

        When(x => x.Type == MetricType.Quantitative, () =>
        {
            RuleFor(x => x.QuantitativeType)
                .NotNull().WithMessage("Tipo quantitativo é obrigatório para métricas quantitativas.")
                .IsInEnum().WithMessage("Tipo quantitativo inválido.");

            RuleFor(x => x.Unit)
                .NotNull().WithMessage("Unidade é obrigatória para métricas quantitativas.")
                .IsInEnum().WithMessage("Unidade inválida.");

            // KeepAbove: requires MinValue
            When(x => x.QuantitativeType == QuantitativeMetricType.KeepAbove, () =>
            {
                RuleFor(x => x.MinValue)
                    .NotNull().WithMessage("Valor mínimo é obrigatório para métricas KeepAbove.")
                    .GreaterThanOrEqualTo(0).WithMessage("Valor mínimo deve ser maior ou igual a 0.");
            });

            // KeepBelow: requires MaxValue
            When(x => x.QuantitativeType == QuantitativeMetricType.KeepBelow, () =>
            {
                RuleFor(x => x.MaxValue)
                    .NotNull().WithMessage("Valor máximo é obrigatório para métricas KeepBelow.")
                    .GreaterThanOrEqualTo(0).WithMessage("Valor máximo deve ser maior ou igual a 0.");
            });

            // KeepBetween: requires both MinValue and MaxValue, with MinValue < MaxValue
            When(x => x.QuantitativeType == QuantitativeMetricType.KeepBetween, () =>
            {
                RuleFor(x => x.MinValue)
                    .NotNull().WithMessage("Valor mínimo é obrigatório para métricas KeepBetween.")
                    .GreaterThanOrEqualTo(0).WithMessage("Valor mínimo deve ser maior ou igual a 0.");

                RuleFor(x => x.MaxValue)
                    .NotNull().WithMessage("Valor máximo é obrigatório para métricas KeepBetween.")
                    .GreaterThanOrEqualTo(0).WithMessage("Valor máximo deve ser maior ou igual a 0.")
                    .GreaterThan(x => x.MinValue ?? 0).WithMessage("Valor máximo deve ser maior que o valor mínimo.");
            });

            // Achieve: requires MaxValue (target to achieve)
            When(x => x.QuantitativeType == QuantitativeMetricType.Achieve, () =>
            {
                RuleFor(x => x.MaxValue)
                    .NotNull().WithMessage("Valor máximo é obrigatório para métricas Achieve.")
                    .GreaterThanOrEqualTo(0).WithMessage("Valor máximo deve ser maior ou igual a 0.");
            });

            // Reduce: requires MaxValue (target to reduce to)
            When(x => x.QuantitativeType == QuantitativeMetricType.Reduce, () =>
            {
                RuleFor(x => x.MaxValue)
                    .NotNull().WithMessage("Valor máximo é obrigatório para métricas Reduce.")
                    .GreaterThanOrEqualTo(0).WithMessage("Valor máximo deve ser maior ou igual a 0.");
            });
        });
    }
}
