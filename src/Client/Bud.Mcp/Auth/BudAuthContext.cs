using Bud.Shared.Contracts;

namespace Bud.Mcp.Auth;

public sealed record BudAuthContext(
    string Token,
    string Email,
    string DisplayName,
    bool IsGlobalAdmin,
    Guid? CollaboratorId,
    Guid? OrganizationId)
{
    public static BudAuthContext FromResponse(SessionResponse response)
    {
        return new BudAuthContext(
            response.Token,
            response.Email,
            response.DisplayName,
            response.IsGlobalAdmin,
            response.CollaboratorId,
            response.OrganizationId);
    }
}
