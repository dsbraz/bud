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
            .NotEmpty().WithMessage("FullName is required.")
            .MaximumLength(200).WithMessage("FullName must not exceed 200 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(320).WithMessage("Email must not exceed 320 characters.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MustAsync(BeUniqueEmail).WithMessage("Email is already in use.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Role must be a valid collaborator role.");

        RuleFor(x => x.LeaderId)
            .MustAsync(BeValidLeader).WithMessage("Leader must exist, belong to the same organization, and have a Leader role.")
            .When(x => x.LeaderId.HasValue);
    }

    private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
            return true;

        var normalizedEmail = email.ToLowerInvariant();
        var exists = await _context.Collaborators
            .IgnoreQueryFilters()
            .AnyAsync(c => c.Email.ToLower() == normalizedEmail, cancellationToken);
        return !exists;
    }

    private async Task<bool> BeValidLeader(CreateCollaboratorRequest request, Guid? leaderId, CancellationToken cancellationToken)
    {
        if (!leaderId.HasValue)
            return true;

        // Obter OrganizationId do TenantProvider
        var currentOrgId = _tenantProvider.TenantId;
        if (!currentOrgId.HasValue)
            return false;

        var leader = await _context.Collaborators
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == leaderId.Value, cancellationToken);

        if (leader == null)
            return false;

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
            .NotEmpty().WithMessage("FullName is required.")
            .MaximumLength(200).WithMessage("FullName must not exceed 200 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(320).WithMessage("Email must not exceed 320 characters.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Role must be a valid collaborator role.");

        RuleFor(x => x.LeaderId)
            .MustAsync(BeValidLeaderForUpdate).WithMessage("Leader must exist, belong to the same organization, and have a Leader role.")
            .When(x => x.LeaderId.HasValue);
    }

    private async Task<bool> BeValidLeaderForUpdate(Guid? leaderId, CancellationToken cancellationToken)
    {
        if (!leaderId.HasValue)
            return true;

        var leader = await _context.Collaborators
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == leaderId.Value, cancellationToken);

        if (leader == null)
            return false;

        return leader.Role == CollaboratorRole.Leader;
    }
}
