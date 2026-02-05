using Bud.Shared.Contracts;
using FluentValidation;

namespace Bud.Server.Validators;

public sealed class CreateMetricCheckinValidator : AbstractValidator<CreateMetricCheckinRequest>
{
    public CreateMetricCheckinValidator()
    {
        RuleFor(x => x.MissionMetricId)
            .NotEmpty().WithMessage("Métrica é obrigatória.");

        RuleFor(x => x.CheckinDate)
            .NotEmpty().WithMessage("Data do check-in é obrigatória.");

        RuleFor(x => x.ConfidenceLevel)
            .InclusiveBetween(1, 5).WithMessage("Nível de confiança deve ser entre 1 e 5.");

        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage("Nota deve ter no máximo 1000 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Note));

        RuleFor(x => x.Text)
            .MaximumLength(1000).WithMessage("Texto deve ter no máximo 1000 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Text));
    }
}

public sealed class UpdateMetricCheckinValidator : AbstractValidator<UpdateMetricCheckinRequest>
{
    public UpdateMetricCheckinValidator()
    {
        RuleFor(x => x.CheckinDate)
            .NotEmpty().WithMessage("Data do check-in é obrigatória.");

        RuleFor(x => x.ConfidenceLevel)
            .InclusiveBetween(1, 5).WithMessage("Nível de confiança deve ser entre 1 e 5.");

        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage("Nota deve ter no máximo 1000 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Note));

        RuleFor(x => x.Text)
            .MaximumLength(1000).WithMessage("Texto deve ter no máximo 1000 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Text));
    }
}
