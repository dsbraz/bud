using Bud.Server.Data;
using Bud.Server.Settings;
using Bud.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Bud.Server.Services;

public sealed class AuthService(ApplicationDbContext dbContext, IOptions<GlobalAdminSettings> globalAdminSettings) : IAuthService
{
    private readonly string _globalAdminEmail = globalAdminSettings.Value.Email;

    public async Task<ServiceResult<AuthLoginResponse>> LoginAsync(AuthLoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            return ServiceResult<AuthLoginResponse>.Failure("Informe o e-mail.");
        }

        var normalizedEmail = email.ToLowerInvariant();

        if (IsGlobalAdminLogin(normalizedEmail))
        {
            return ServiceResult<AuthLoginResponse>.Success(new AuthLoginResponse
            {
                Email = normalizedEmail,
                DisplayName = "Administrador Global",
                IsGlobalAdmin = true
            });
        }

        var collaborator = await dbContext.Collaborators
            .AsNoTracking()
            .IgnoreQueryFilters() // During login, we need to bypass tenant filters
            .FirstOrDefaultAsync(c => c.Email == normalizedEmail, cancellationToken);

        if (collaborator is null)
        {
            return ServiceResult<AuthLoginResponse>.NotFound("Usuário não encontrado.");
        }

        return ServiceResult<AuthLoginResponse>.Success(new AuthLoginResponse
        {
            Email = collaborator.Email,
            DisplayName = collaborator.FullName,
            IsGlobalAdmin = false,
            CollaboratorId = collaborator.Id,
            Role = collaborator.Role,
            OrganizationId = collaborator.OrganizationId
        });
    }

    public async Task<ServiceResult<List<OrganizationSummaryDto>>> GetMyOrganizationsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return ServiceResult<List<OrganizationSummaryDto>>.Failure("Email is required.");
        }

        // Global admin can see all organizations
        if (IsGlobalAdminLogin(normalizedEmail))
        {
            var allOrgs = await dbContext.Organizations
                .AsNoTracking()
                .IgnoreQueryFilters() // Global admin needs to see all orgs to populate dropdown
                .OrderBy(o => o.Name)
                .Select(o => new OrganizationSummaryDto
                {
                    Id = o.Id,
                    Name = o.Name
                })
                .ToListAsync(cancellationToken);

            return ServiceResult<List<OrganizationSummaryDto>>.Success(allOrgs);
        }

        // Regular users: get organizations from three sources:
        // 1. Organizations where they are members (via Collaborator → Organization directly)
        // 2. Organizations where they are the Owner

        var orgsFromMembership = await dbContext.Collaborators
            .AsNoTracking()
            .IgnoreQueryFilters() // Need to bypass filters to discover user's organizations
            .Where(c => c.Email == normalizedEmail)
            .Include(c => c.Organization)
            .Select(c => new OrganizationSummaryDto
            {
                Id = c.Organization.Id,
                Name = c.Organization.Name
            })
            .ToListAsync(cancellationToken);

        var orgsFromOwnership = await dbContext.Organizations
            .AsNoTracking()
            .IgnoreQueryFilters() // Need to bypass filters to discover owned organizations
            .Where(o => o.Owner != null && o.Owner.Email == normalizedEmail)
            .Select(o => new OrganizationSummaryDto
            {
                Id = o.Id,
                Name = o.Name
            })
            .ToListAsync(cancellationToken);

        // Combine and deduplicate
        var organizations = orgsFromMembership
            .Concat(orgsFromOwnership)
            .GroupBy(o => o.Id)
            .Select(g => g.First())
            .OrderBy(o => o.Name)
            .ToList();

        return ServiceResult<List<OrganizationSummaryDto>>.Success(organizations);
    }

    private bool IsGlobalAdminLogin(string normalizedEmail)
    {
        return string.Equals(normalizedEmail, _globalAdminEmail, StringComparison.OrdinalIgnoreCase);
    }
}
