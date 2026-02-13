using Bud.Server.Services;
using Bud.Shared.Contracts;
using FluentValidation;

namespace Bud.Server.Validators;

public sealed class CreateCollaboratorValidator : AbstractValidator<CreateCollaboratorRequest>
{
    private readonly ICollaboratorValidationService _validationService;

    public CreateCollaboratorValidator(ICollaboratorValidationService validationService)
    {
        _validationService = validationService;

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Nome completo é obrigatório.")
            .MaximumLength(200).WithMessage("Nome completo deve ter no máximo 200 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail é obrigatório.")
            .MaximumLength(320).WithMessage("E-mail deve ter no máximo 320 caracteres.")
            .EmailAddress().WithMessage("E-mail deve ser válido.")
            .MustAsync(BeUniqueEmail).WithMessage("E-mail já está em uso.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Função inválida.");

        RuleFor(x => x.LeaderId)
            .MustAsync(BeValidLeader).WithMessage("O líder deve existir, pertencer à mesma organização e ter a função de Líder.")
            .When(x => x.LeaderId.HasValue);
    }

    private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
    {
        return await _validationService.IsEmailUniqueAsync(email, cancellationToken);
    }

    private async Task<bool> BeValidLeader(CreateCollaboratorRequest _, Guid? leaderId, CancellationToken cancellationToken)
    {
        if (!leaderId.HasValue)
        {
            return true;
        }

        return await _validationService.IsValidLeaderForCreateAsync(leaderId.Value, cancellationToken);
    }
}

public sealed class UpdateCollaboratorValidator : AbstractValidator<UpdateCollaboratorRequest>
{
    private readonly ICollaboratorValidationService _validationService;

    public UpdateCollaboratorValidator(ICollaboratorValidationService validationService)
    {
        _validationService = validationService;

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Nome completo é obrigatório.")
            .MaximumLength(200).WithMessage("Nome completo deve ter no máximo 200 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail é obrigatório.")
            .MaximumLength(320).WithMessage("E-mail deve ter no máximo 320 caracteres.")
            .EmailAddress().WithMessage("E-mail deve ser válido.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Função inválida.");

        RuleFor(x => x.LeaderId)
            .MustAsync(BeValidLeaderForUpdate).WithMessage("O líder deve existir, pertencer à mesma organização e ter a função de Líder.")
            .When(x => x.LeaderId.HasValue);
    }

    private async Task<bool> BeValidLeaderForUpdate(Guid? leaderId, CancellationToken cancellationToken)
    {
        if (!leaderId.HasValue)
        {
            return true;
        }

        return await _validationService.IsValidLeaderForUpdateAsync(leaderId.Value, cancellationToken);
    }
}
