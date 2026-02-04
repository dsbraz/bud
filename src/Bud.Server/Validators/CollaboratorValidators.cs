using Bud.Server.Data;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Validators;

public sealed class CreateCollaboratorValidator : AbstractValidator<CreateCollaboratorRequest>
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public CreateCollaboratorValidator(ApplicationDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;

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
        if (string.IsNullOrWhiteSpace(email))
        {
            return true;
        }

#pragma warning disable CA1304, CA1311
        var normalizedEmail = email.ToLower();
#pragma warning restore CA1304, CA1311
#pragma warning disable CA1304, CA1311, CA1862
        var exists = await _context.Collaborators
            .IgnoreQueryFilters()
            .AnyAsync(c => c.Email.ToLower() == normalizedEmail, cancellationToken);
#pragma warning restore CA1304, CA1311, CA1862
        return !exists;
    }

    private async Task<bool> BeValidLeader(CreateCollaboratorRequest request, Guid? leaderId, CancellationToken cancellationToken)
    {
        if (!leaderId.HasValue)
        {
            return true;
        }

        // Obter OrganizationId do TenantProvider
        var currentOrgId = _tenantProvider.TenantId;
        if (!currentOrgId.HasValue)
        {
            return false;
        }

        var leader = await _context.Collaborators
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == leaderId.Value, cancellationToken);

        if (leader == null)
        {
            return false;
        }

        // Validar que o líder pertence à mesma organização e tem role Leader
        return leader.OrganizationId == currentOrgId.Value && leader.Role == CollaboratorRole.Leader;
    }
}

public sealed class UpdateCollaboratorValidator : AbstractValidator<UpdateCollaboratorRequest>
{
    private readonly ApplicationDbContext _context;

    public UpdateCollaboratorValidator(ApplicationDbContext context)
    {
        _context = context;

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

        var leader = await _context.Collaborators
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == leaderId.Value, cancellationToken);

        if (leader == null)
        {
            return false;
        }

        return leader.Role == CollaboratorRole.Leader;
    }
}
