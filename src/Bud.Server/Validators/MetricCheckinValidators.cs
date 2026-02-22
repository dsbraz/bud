using Bud.Shared.Contracts;
using FluentValidation;

namespace Bud.Server.Validators;

public sealed class CreateMetricCheckinValidator : AbstractValidator<CreateCheckinRequest>
{
    public CreateMetricCheckinValidator()
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

public sealed class PatchMetricCheckinValidator : AbstractValidator<PatchCheckinRequest>
{
    public PatchMetricCheckinValidator()
    {
        RuleFor(x => x.CheckinDate.Value)
            .NotEmpty().WithMessage("Data do check-in é obrigatória.");

        RuleFor(x => x.ConfidenceLevel.Value)
            .InclusiveBetween(1, 5).WithMessage("Nível de confiança deve ser entre 1 e 5.");

        RuleFor(x => x.Note.Value)
            .MaximumLength(1000).WithMessage("Nota deve ter no máximo 1000 caracteres.")
            .When(x => x.Note.HasValue && !string.IsNullOrEmpty(x.Note.Value));

        RuleFor(x => x.Text.Value)
            .MaximumLength(1000).WithMessage("Texto deve ter no máximo 1000 caracteres.")
            .When(x => x.Text.HasValue && !string.IsNullOrEmpty(x.Text.Value));
    }
}
