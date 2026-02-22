using Bud.Shared.Contracts;
using FluentValidation;

namespace Bud.Server.Validators;

public sealed class CreateMissionTemplateValidator : AbstractValidator<CreateTemplateRequest>
{
    public CreateMissionTemplateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Descrição deve ter no máximo 1000 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.MissionNamePattern)
            .MaximumLength(200).WithMessage("Padrão de nome deve ter no máximo 200 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.MissionNamePattern));

        RuleFor(x => x.MissionDescriptionPattern)
            .MaximumLength(1000).WithMessage("Padrão de descrição deve ter no máximo 1000 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.MissionDescriptionPattern));

        RuleForEach(x => x.Objectives)
            .SetValidator(new MissionTemplateObjectiveDtoValidator());

        RuleForEach(x => x.Metrics)
            .SetValidator(new MissionTemplateMetricDtoValidator());

        RuleFor(x => x)
            .Must(HaveValidObjectiveReferences)
            .WithMessage("Uma ou mais métricas referenciam objetivos inexistentes no template.");
    }

    private static bool HaveValidObjectiveReferences(CreateTemplateRequest request)
    {
        var objectiveIds = request.Objectives
            .Where(o => o.Id.HasValue)
            .Select(o => o.Id!.Value)
            .ToHashSet();

        return request.Metrics.All(metric =>
            metric.TemplateObjectiveId is null || objectiveIds.Contains(metric.TemplateObjectiveId.Value));
    }
}

public sealed class PatchMissionTemplateValidator : AbstractValidator<PatchTemplateRequest>
{
    public PatchMissionTemplateValidator()
    {
        RuleFor(x => x.Name.Value)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres.")
            .When(x => x.Name.HasValue);

        RuleFor(x => x.Description.Value)
            .MaximumLength(1000).WithMessage("Descrição deve ter no máximo 1000 caracteres.")
            .When(x => x.Description.HasValue && !string.IsNullOrEmpty(x.Description.Value));

        RuleFor(x => x.MissionNamePattern.Value)
            .MaximumLength(200).WithMessage("Padrão de nome deve ter no máximo 200 caracteres.")
            .When(x => x.MissionNamePattern.HasValue && !string.IsNullOrEmpty(x.MissionNamePattern.Value));

        RuleFor(x => x.MissionDescriptionPattern.Value)
            .MaximumLength(1000).WithMessage("Padrão de descrição deve ter no máximo 1000 caracteres.")
            .When(x => x.MissionDescriptionPattern.HasValue && !string.IsNullOrEmpty(x.MissionDescriptionPattern.Value));

        When(x => x.Objectives.HasValue && x.Objectives.Value is not null, () =>
        {
            RuleForEach(x => x.Objectives.Value!)
                .SetValidator(new MissionTemplateObjectiveDtoValidator());
        });

        When(x => x.Metrics.HasValue && x.Metrics.Value is not null, () =>
        {
            RuleForEach(x => x.Metrics.Value!)
                .SetValidator(new MissionTemplateMetricDtoValidator());
        });

        RuleFor(x => x)
            .Must(HaveValidObjectiveReferences)
            .WithMessage("Uma ou mais métricas referenciam objetivos inexistentes no template.");
    }

    private static bool HaveValidObjectiveReferences(PatchTemplateRequest request)
    {
        var objectiveIds = request.Objectives.AsEnumerable()
            .Where(o => o.Id.HasValue)
            .Select(o => o.Id!.Value)
            .ToHashSet();

        return request.Metrics.AsEnumerable().All(metric =>
            metric.TemplateObjectiveId is null || objectiveIds.Contains(metric.TemplateObjectiveId.Value));
    }
}

public sealed class MissionTemplateObjectiveDtoValidator : AbstractValidator<TemplateObjectiveRequest>
{
    public MissionTemplateObjectiveDtoValidator()
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

        RuleFor(x => x.OrderIndex)
            .GreaterThanOrEqualTo(0).WithMessage("Índice de ordenação deve ser maior ou igual a 0.");
    }
}

public sealed class MissionTemplateMetricDtoValidator : AbstractValidator<TemplateMetricRequest>
{
    public MissionTemplateMetricDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Tipo inválido.");

        RuleFor(x => x.OrderIndex)
            .GreaterThanOrEqualTo(0).WithMessage("Índice de ordenação deve ser maior ou igual a 0.");

        When(x => x.Type == MetricType.Qualitative, () =>
        {
            RuleFor(x => x.TargetText)
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

            When(x => x.QuantitativeType == QuantitativeMetricType.KeepAbove, () =>
            {
                RuleFor(x => x.MinValue)
                    .NotNull().WithMessage("Valor mínimo é obrigatório para métricas KeepAbove.")
                    .GreaterThanOrEqualTo(0).WithMessage("Valor mínimo deve ser maior ou igual a 0.");
            });

            When(x => x.QuantitativeType == QuantitativeMetricType.KeepBelow, () =>
            {
                RuleFor(x => x.MaxValue)
                    .NotNull().WithMessage("Valor máximo é obrigatório para métricas KeepBelow.")
                    .GreaterThanOrEqualTo(0).WithMessage("Valor máximo deve ser maior ou igual a 0.");
            });

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

            When(x => x.QuantitativeType == QuantitativeMetricType.Achieve, () =>
            {
                RuleFor(x => x.MaxValue)
                    .NotNull().WithMessage("Valor máximo é obrigatório para métricas Achieve.")
                    .GreaterThanOrEqualTo(0).WithMessage("Valor máximo deve ser maior ou igual a 0.");
            });

            When(x => x.QuantitativeType == QuantitativeMetricType.Reduce, () =>
            {
                RuleFor(x => x.MaxValue)
                    .NotNull().WithMessage("Valor máximo é obrigatório para métricas Reduce.")
                    .GreaterThanOrEqualTo(0).WithMessage("Valor máximo deve ser maior ou igual a 0.");
            });
        });
    }
}
