using Bud.Server.Data;
using Bud.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Services;

public sealed class AuthService(ApplicationDbContext dbContext) : IAuthService
{
    private const string AdminAlias = "admin";

    public async Task<ServiceResult<AuthLoginResponse>> LoginAsync(AuthLoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            return ServiceResult<AuthLoginResponse>.Failure("Informe o e-mail.");
        }

        var normalizedEmail = email.ToLowerInvariant();

        if (IsAdminLogin(normalizedEmail))
        {
            return ServiceResult<AuthLoginResponse>.Success(new AuthLoginResponse
            {
                Email = normalizedEmail,
                DisplayName = "Administrador",
                IsAdmin = true
            });
        }

        var collaborator = await dbContext.Collaborators
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Email == normalizedEmail, cancellationToken);

        if (collaborator is null)
        {
            return ServiceResult<AuthLoginResponse>.NotFound("Usuário não encontrado.");
        }

        return ServiceResult<AuthLoginResponse>.Success(new AuthLoginResponse
        {
            Email = collaborator.Email,
            DisplayName = collaborator.FullName,
            IsAdmin = false,
            CollaboratorId = collaborator.Id,
            Role = collaborator.Role
        });
    }

    private static bool IsAdminLogin(string normalizedEmail)
    {
        return string.Equals(normalizedEmail, AdminAlias, StringComparison.OrdinalIgnoreCase)
            || normalizedEmail.StartsWith($"{AdminAlias}@", StringComparison.OrdinalIgnoreCase);
    }
}
