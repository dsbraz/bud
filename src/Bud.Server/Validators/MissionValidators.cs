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
            RuleFor(x => x.TargetValue)
                .NotNull().WithMessage("TargetValue is required for quantitative metrics.")
                .GreaterThanOrEqualTo(0).WithMessage("TargetValue must be greater than or equal to 0.");

            RuleFor(x => x.Unit)
                .NotNull().WithMessage("Unit is required for quantitative metrics.")
                .IsInEnum().WithMessage("Unit is invalid.");
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
            RuleFor(x => x.TargetValue)
                .NotNull().WithMessage("TargetValue is required for quantitative metrics.")
                .GreaterThanOrEqualTo(0).WithMessage("TargetValue must be greater than or equal to 0.");

            RuleFor(x => x.Unit)
                .NotNull().WithMessage("Unit is required for quantitative metrics.")
                .IsInEnum().WithMessage("Unit is invalid.");
        });
    }
}
