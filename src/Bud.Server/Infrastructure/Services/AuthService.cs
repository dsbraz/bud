using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Bud.Server.Application.Common;
using Bud.Server.Application.Ports;
using Bud.Server.Domain.ReadModels;
using Bud.Server.Infrastructure.Persistence;
using Bud.Server.Settings;
using Bud.Shared.Contracts;
using Bud.Server.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Bud.Server.Infrastructure.Services;

public sealed class AuthService(
    ApplicationDbContext dbContext,
    IOptions<JwtSettings> jwtOptions) : IAuthService
{
    private readonly JwtSettings _jwtSettings = jwtOptions.Value;

    public async Task<Result<AuthLoginResult>> LoginAsync(AuthLoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            return Result<AuthLoginResult>.Failure("Informe o e-mail.");
        }

        var normalizedEmail = email.ToLowerInvariant();

        var collaborator = await dbContext.Collaborators
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Email == normalizedEmail, cancellationToken);

        if (collaborator is null)
        {
            return Result<AuthLoginResult>.NotFound("Usuário não encontrado.");
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

        return Result<AuthLoginResult>.Success(new AuthLoginResult
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

    public async Task<Result<List<OrganizationSummary>>> GetMyOrganizationsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return Result<List<OrganizationSummary>>.Failure("E-mail é obrigatório.");
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
                .Select(o => new OrganizationSummary
                {
                    Id = o.Id,
                    Name = o.Name
                })
                .ToListAsync(cancellationToken);

            return Result<List<OrganizationSummary>>.Success(allOrgs);
        }

        // Regular users: get organizations from two sources:
        // 1. Organizations where they are members (via Collaborator -> Organization directly)
        // 2. Organizations where they are the Owner

        var orgsFromMembership = await dbContext.Collaborators
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(c => c.Email == normalizedEmail)
            .Include(c => c.Organization)
            .Select(c => new OrganizationSummary
            {
                Id = c.Organization.Id,
                Name = c.Organization.Name
            })
            .ToListAsync(cancellationToken);

        var orgsFromOwnership = await dbContext.Organizations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => o.Owner != null && o.Owner.Email == normalizedEmail)
            .Select(o => new OrganizationSummary
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

        return Result<List<OrganizationSummary>>.Success(organizations);
    }

    private async Task RegisterAccessLogAsync(Guid collaboratorId, Guid organizationId, CancellationToken cancellationToken)
    {
        dbContext.CollaboratorAccessLogs.Add(
            CollaboratorAccessLog.Create(Guid.NewGuid(), collaboratorId, organizationId, DateTime.UtcNow));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private string GenerateJwtToken(List<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_jwtSettings.TokenExpirationHours),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
