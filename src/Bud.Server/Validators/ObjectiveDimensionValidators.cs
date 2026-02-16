using Bud.Shared.Contracts;
using FluentValidation;

namespace Bud.Server.Validators;

public sealed class CreateObjectiveDimensionValidator : AbstractValidator<CreateObjectiveDimensionRequest>
{
    public CreateObjectiveDimensionValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres.");
    }
}

public sealed class UpdateObjectiveDimensionValidator : AbstractValidator<UpdateObjectiveDimensionRequest>
{
    public UpdateObjectiveDimensionValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres.");
    }
}
