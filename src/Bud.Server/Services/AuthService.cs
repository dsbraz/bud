using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Bud.Server.Data;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Bud.Server.Services;

public sealed class AuthService(
    ApplicationDbContext dbContext,
    IConfiguration configuration) : IAuthService
{
    public async Task<ServiceResult<AuthLoginResponse>> LoginAsync(AuthLoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            return ServiceResult<AuthLoginResponse>.Failure("Informe o e-mail.");
        }

        var normalizedEmail = email.ToLowerInvariant();

        var collaborator = await dbContext.Collaborators
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Email == normalizedEmail, cancellationToken);

        if (collaborator is null)
        {
            return ServiceResult<AuthLoginResponse>.NotFound("Usuário não encontrado.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, collaborator.Email),
            new("email", collaborator.Email),
            new("collaborator_id", collaborator.Id.ToString()),
            new("organization_id", collaborator.OrganizationId.ToString()),
            new(ClaimTypes.Name, collaborator.FullName)
        };

        if (collaborator.IsGlobalAdmin)
        {
            claims.Add(new(ClaimTypes.Role, "GlobalAdmin"));
        }

        await RegisterAccessLogAsync(collaborator.Id, collaborator.OrganizationId, cancellationToken);

        var token = GenerateJwtToken(claims);

        return ServiceResult<AuthLoginResponse>.Success(new AuthLoginResponse
        {
            Token = token,
            Email = collaborator.Email,
            DisplayName = collaborator.FullName,
            IsGlobalAdmin = collaborator.IsGlobalAdmin,
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

        var collaborator = await dbContext.Collaborators
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Email == normalizedEmail, cancellationToken);

        if (collaborator?.IsGlobalAdmin == true)
        {
            var allOrgs = await dbContext.Organizations
                .AsNoTracking()
                .IgnoreQueryFilters()
                .OrderBy(o => o.Name)
                .Select(o => new OrganizationSummaryDto
                {
                    Id = o.Id,
                    Name = o.Name
                })
                .ToListAsync(cancellationToken);

            return ServiceResult<List<OrganizationSummaryDto>>.Success(allOrgs);
        }

        // Regular users: get organizations from two sources:
        // 1. Organizations where they are members (via Collaborator → Organization directly)
        // 2. Organizations where they are the Owner

        var orgsFromMembership = await dbContext.Collaborators
            .AsNoTracking()
            .IgnoreQueryFilters()
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
            .IgnoreQueryFilters()
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

    private async Task RegisterAccessLogAsync(Guid collaboratorId, Guid organizationId, CancellationToken cancellationToken)
    {
        dbContext.CollaboratorAccessLogs.Add(
            CollaboratorAccessLog.Create(Guid.NewGuid(), collaboratorId, organizationId, DateTime.UtcNow));
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
