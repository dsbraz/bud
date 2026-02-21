using Bud.Shared.Contracts;
using FluentValidation;

namespace Bud.Server.Validators;

public sealed class CreateMissionTemplateValidator : AbstractValidator<CreateMissionTemplateRequest>
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

    private static bool HaveValidObjectiveReferences(CreateMissionTemplateRequest request)
    {
        var objectiveIds = request.Objectives
            .Where(o => o.Id.HasValue)
            .Select(o => o.Id!.Value)
            .ToHashSet();

        return request.Metrics.All(metric =>
            metric.MissionTemplateObjectiveId is null || objectiveIds.Contains(metric.MissionTemplateObjectiveId.Value));
    }
}

public sealed class UpdateMissionTemplateValidator : AbstractValidator<UpdateMissionTemplateRequest>
{
    public UpdateMissionTemplateValidator()
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

    private static bool HaveValidObjectiveReferences(UpdateMissionTemplateRequest request)
    {
        var objectiveIds = request.Objectives
            .Where(o => o.Id.HasValue)
            .Select(o => o.Id!.Value)
            .ToHashSet();

        return request.Metrics.All(metric =>
            metric.MissionTemplateObjectiveId is null || objectiveIds.Contains(metric.MissionTemplateObjectiveId.Value));
    }
}

public sealed class MissionTemplateObjectiveDtoValidator : AbstractValidator<MissionTemplateObjectiveDto>
{
    public MissionTemplateObjectiveDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Descrição deve ter no máximo 1000 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.OrderIndex)
            .GreaterThanOrEqualTo(0).WithMessage("Índice de ordenação deve ser maior ou igual a 0.");
    }
}

public sealed class MissionTemplateMetricDtoValidator : AbstractValidator<MissionTemplateMetricDto>
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
