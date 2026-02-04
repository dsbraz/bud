using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Bud.Server.IntegrationTests.Helpers;

public static class JwtTestHelper
{
    private const string SecretKey = "CHANGE-THIS-KEY-IN-PRODUCTION-USE-AT-LEAST-32-CHARACTERS";
    private const string Issuer = "bud-api";
    private const string Audience = "bud-api";

    public static string GenerateGlobalAdminToken()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, "admin@getbud.co"),
            new("email", "admin@getbud.co"),
            new(ClaimTypes.Role, "GlobalAdmin")
        };

        return GenerateToken(claims);
    }

    public static string GenerateTenantUserToken(
        string email,
        Guid organizationId,
        Guid collaboratorId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email),
            new("email", email),
            new("organization_id", organizationId.ToString()),
            new("collaborator_id", collaboratorId.ToString())
        };

        return GenerateToken(claims);
    }

    private static string GenerateToken(List<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
