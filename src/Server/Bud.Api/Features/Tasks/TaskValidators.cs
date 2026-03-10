using Bud.Shared.Contracts;
using FluentValidation;

namespace Bud.Api.Features.Tasks;

public sealed class CreateTaskValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskValidator()
    {
        RuleFor(x => x.GoalId)
            .NotEmpty().WithMessage("Meta é obrigatória.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres.");

        RuleFor(x => x.State)
            .IsInEnum().WithMessage("Estado inválido.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Descrição deve ter no máximo 2000 caracteres.")
            .When(x => x.Description is not null);
    }
}

public sealed class PatchTaskValidator : AbstractValidator<PatchTaskRequest>
{
    public PatchTaskValidator()
    {
        RuleFor(x => x.Name.Value)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres.")
            .When(x => x.Name.HasValue);

        RuleFor(x => x.State.Value)
            .IsInEnum().WithMessage("Estado inválido.")
            .When(x => x.State.HasValue);

        RuleFor(x => x.Description.Value)
            .MaximumLength(2000).WithMessage("Descrição deve ter no máximo 2000 caracteres.")
            .When(x => x.Description.HasValue && !string.IsNullOrEmpty(x.Description.Value));
    }
}
