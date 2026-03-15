using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Bud.Application.Common;
using Bud.Application.Configuration;
using Bud.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Bud.Infrastructure.Sessions;

public sealed class SessionAuthenticator(
    ApplicationDbContext dbContext,
    IOptions<JwtSettings> jwtOptions) : ISessionAuthenticator
{
    private readonly JwtSettings _jwtSettings = jwtOptions.Value;

    public async Task<Result<LoginResult>> LoginAsync(CreateSessionRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            return Result<LoginResult>.Failure("Informe o e-mail.");
        }

        var normalizedEmail = email.ToLowerInvariant();

        var collaborator = await dbContext.Collaborators
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Email == normalizedEmail, cancellationToken);

        if (collaborator is null)
        {
            return Result<LoginResult>.NotFound("Usuário não encontrado.");
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

        return Result<LoginResult>.Success(new LoginResult
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
