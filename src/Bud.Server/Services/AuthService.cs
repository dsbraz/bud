using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Bud.Server.Data;
using Bud.Server.Settings;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Bud.Server.Services;

public sealed class AuthService(
    ApplicationDbContext dbContext,
    IOptions<GlobalAdminSettings> globalAdminSettings,
    IConfiguration configuration) : IAuthService
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
            var adminClaims = new List<Claim>
            {
                new(ClaimTypes.Email, normalizedEmail),
                new("email", normalizedEmail),
                new(ClaimTypes.Role, "GlobalAdmin")
            };

            var adminCollaborator = await dbContext.Collaborators
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Email == normalizedEmail, cancellationToken);

            Guid? adminCollaboratorId = null;
            Guid? adminOrgId = null;
            var displayName = "Administrador Global";

            if (adminCollaborator is not null)
            {
                adminClaims.Add(new("collaborator_id", adminCollaborator.Id.ToString()));
                adminClaims.Add(new("organization_id", adminCollaborator.OrganizationId.ToString()));
                adminClaims.Add(new(ClaimTypes.Name, adminCollaborator.FullName));
                adminCollaboratorId = adminCollaborator.Id;
                adminOrgId = adminCollaborator.OrganizationId;
                displayName = adminCollaborator.FullName;
            }

            if (adminCollaborator is not null)
            {
                await RegisterAccessLogAsync(adminCollaborator.Id, adminCollaborator.OrganizationId, cancellationToken);
            }

            var adminToken = GenerateJwtToken(adminClaims);

            return ServiceResult<AuthLoginResponse>.Success(new AuthLoginResponse
            {
                Token = adminToken,
                Email = normalizedEmail,
                DisplayName = displayName,
                IsGlobalAdmin = true,
                CollaboratorId = adminCollaboratorId,
                OrganizationId = adminOrgId
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

        await RegisterAccessLogAsync(collaborator.Id, collaborator.OrganizationId, cancellationToken);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, collaborator.Email),
            new("email", collaborator.Email),
            new("collaborator_id", collaborator.Id.ToString()),
            new("organization_id", collaborator.OrganizationId.ToString()),
            new(ClaimTypes.Name, collaborator.FullName)
        };

        var token = GenerateJwtToken(claims);

        return ServiceResult<AuthLoginResponse>.Success(new AuthLoginResponse
        {
            Token = token,
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
            return ServiceResult<List<OrganizationSummaryDto>>.Failure("E-mail é obrigatório.");
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

    private async Task RegisterAccessLogAsync(Guid collaboratorId, Guid organizationId, CancellationToken cancellationToken)
    {
        dbContext.CollaboratorAccessLogs.Add(new CollaboratorAccessLog
        {
            Id = Guid.NewGuid(),
            CollaboratorId = collaboratorId,
            OrganizationId = organizationId,
            AccessedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private string GenerateJwtToken(List<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            configuration["Jwt:Key"] ?? "dev-secret-key-change-in-production-minimum-32-characters-required"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"] ?? "bud-dev",
            audience: configuration["Jwt:Audience"] ?? "bud-api",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
