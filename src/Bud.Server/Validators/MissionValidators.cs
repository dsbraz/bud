using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentValidation;

namespace Bud.Server.Validators;

public sealed class CreateMissionValidator : AbstractValidator<CreateMissionRequest>
{
    public CreateMissionValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("StartDate is required.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("EndDate is required.")
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("EndDate must be on or after StartDate.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status is invalid.");

        RuleFor(x => x.ScopeType)
            .IsInEnum().WithMessage("ScopeType is invalid.");

        RuleFor(x => x.ScopeId)
            .NotEmpty().WithMessage("ScopeId is required.");
    }
}

public sealed class UpdateMissionValidator : AbstractValidator<UpdateMissionRequest>
{
    public UpdateMissionValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("StartDate is required.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("EndDate is required.")
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("EndDate must be on or after StartDate.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status is invalid.");
    }
}

public sealed class CreateMissionMetricValidator : AbstractValidator<CreateMissionMetricRequest>
{
    public CreateMissionMetricValidator()
    {
        RuleFor(x => x.MissionId)
            .NotEmpty().WithMessage("MissionId is required.");

        ApplyMetricRules();
    }

    private void ApplyMetricRules()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Type is invalid.");

        When(x => x.Type == MetricType.Qualitative, () =>
        {
            RuleFor(x => x.TargetText)
                .NotEmpty().WithMessage("TargetText is required for qualitative metrics.")
                .MaximumLength(1000).WithMessage("TargetText must not exceed 1000 characters.");
        });

        When(x => x.Type == MetricType.Quantitative, () =>
        {
            RuleFor(x => x.QuantitativeType)
                .NotNull().WithMessage("QuantitativeType is required for quantitative metrics.")
                .IsInEnum().WithMessage("QuantitativeType is invalid.");

            RuleFor(x => x.Unit)
                .NotNull().WithMessage("Unit is required for quantitative metrics.")
                .IsInEnum().WithMessage("Unit is invalid.");

            // KeepAbove: requires MinValue
            When(x => x.QuantitativeType == QuantitativeMetricType.KeepAbove, () =>
            {
                RuleFor(x => x.MinValue)
                    .NotNull().WithMessage("MinValue is required for KeepAbove metrics.")
                    .GreaterThanOrEqualTo(0).WithMessage("MinValue must be greater than or equal to 0.");
            });

            // KeepBelow: requires MaxValue
            When(x => x.QuantitativeType == QuantitativeMetricType.KeepBelow, () =>
            {
                RuleFor(x => x.MaxValue)
                    .NotNull().WithMessage("MaxValue is required for KeepBelow metrics.")
                    .GreaterThanOrEqualTo(0).WithMessage("MaxValue must be greater than or equal to 0.");
            });

            // KeepBetween: requires both MinValue and MaxValue, with MinValue < MaxValue
            When(x => x.QuantitativeType == QuantitativeMetricType.KeepBetween, () =>
            {
                RuleFor(x => x.MinValue)
                    .NotNull().WithMessage("MinValue is required for KeepBetween metrics.")
                    .GreaterThanOrEqualTo(0).WithMessage("MinValue must be greater than or equal to 0.");

                RuleFor(x => x.MaxValue)
                    .NotNull().WithMessage("MaxValue is required for KeepBetween metrics.")
                    .GreaterThanOrEqualTo(0).WithMessage("MaxValue must be greater than or equal to 0.")
                    .GreaterThan(x => x.MinValue ?? 0).WithMessage("MaxValue must be greater than MinValue.");
            });

            // Achieve: requires MaxValue (target to achieve)
            When(x => x.QuantitativeType == QuantitativeMetricType.Achieve, () =>
            {
                RuleFor(x => x.MaxValue)
                    .NotNull().WithMessage("MaxValue is required for Achieve metrics.")
                    .GreaterThanOrEqualTo(0).WithMessage("MaxValue must be greater than or equal to 0.");
            });

            // Reduce: requires MaxValue (target to reduce to)
            When(x => x.QuantitativeType == QuantitativeMetricType.Reduce, () =>
            {
                RuleFor(x => x.MaxValue)
                    .NotNull().WithMessage("MaxValue is required for Reduce metrics.")
                    .GreaterThanOrEqualTo(0).WithMessage("MaxValue must be greater than or equal to 0.");
            });
        });
    }
}

public sealed class UpdateMissionMetricValidator : AbstractValidator<UpdateMissionMetricRequest>
{
    public UpdateMissionMetricValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Type is invalid.");

        When(x => x.Type == MetricType.Qualitative, () =>
        {
            RuleFor(x => x.TargetText)
                .NotEmpty().WithMessage("TargetText is required for qualitative metrics.")
                .MaximumLength(1000).WithMessage("TargetText must not exceed 1000 characters.");
        });

        When(x => x.Type == MetricType.Quantitative, () =>
        {
            RuleFor(x => x.QuantitativeType)
                .NotNull().WithMessage("QuantitativeType is required for quantitative metrics.")
                .IsInEnum().WithMessage("QuantitativeType is invalid.");

            RuleFor(x => x.Unit)
                .NotNull().WithMessage("Unit is required for quantitative metrics.")
                .IsInEnum().WithMessage("Unit is invalid.");

            // KeepAbove: requires MinValue
            When(x => x.QuantitativeType == QuantitativeMetricType.KeepAbove, () =>
            {
                RuleFor(x => x.MinValue)
                    .NotNull().WithMessage("MinValue is required for KeepAbove metrics.")
                    .GreaterThanOrEqualTo(0).WithMessage("MinValue must be greater than or equal to 0.");
            });

            // KeepBelow: requires MaxValue
            When(x => x.QuantitativeType == QuantitativeMetricType.KeepBelow, () =>
            {
                RuleFor(x => x.MaxValue)
                    .NotNull().WithMessage("MaxValue is required for KeepBelow metrics.")
                    .GreaterThanOrEqualTo(0).WithMessage("MaxValue must be greater than or equal to 0.");
            });

            // KeepBetween: requires both MinValue and MaxValue, with MinValue < MaxValue
            When(x => x.QuantitativeType == QuantitativeMetricType.KeepBetween, () =>
            {
                RuleFor(x => x.MinValue)
                    .NotNull().WithMessage("MinValue is required for KeepBetween metrics.")
                    .GreaterThanOrEqualTo(0).WithMessage("MinValue must be greater than or equal to 0.");

                RuleFor(x => x.MaxValue)
                    .NotNull().WithMessage("MaxValue is required for KeepBetween metrics.")
                    .GreaterThanOrEqualTo(0).WithMessage("MaxValue must be greater than or equal to 0.")
                    .GreaterThan(x => x.MinValue ?? 0).WithMessage("MaxValue must be greater than MinValue.");
            });

            // Achieve: requires MaxValue (target to achieve)
            When(x => x.QuantitativeType == QuantitativeMetricType.Achieve, () =>
            {
                RuleFor(x => x.MaxValue)
                    .NotNull().WithMessage("MaxValue is required for Achieve metrics.")
                    .GreaterThanOrEqualTo(0).WithMessage("MaxValue must be greater than or equal to 0.");
            });

            // Reduce: requires MaxValue (target to reduce to)
            When(x => x.QuantitativeType == QuantitativeMetricType.Reduce, () =>
            {
                RuleFor(x => x.MaxValue)
                    .NotNull().WithMessage("MaxValue is required for Reduce metrics.")
                    .GreaterThanOrEqualTo(0).WithMessage("MaxValue must be greater than or equal to 0.");
            });
        });
    }
}
