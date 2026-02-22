using Bud.Server.Domain.ReadModels;

namespace Bud.Server.Application.Mapping;

internal static class SessionsContractMapper
{
    public static SessionResponse ToResponse(this AuthLoginResult source)
    {
        return new SessionResponse
        {
            Token = source.Token,
            Email = source.Email,
            DisplayName = source.DisplayName,
            IsGlobalAdmin = source.IsGlobalAdmin,
            CollaboratorId = source.CollaboratorId,
            Role = source.Role.HasValue ? source.Role.Value.ToShared() : null,
            OrganizationId = source.OrganizationId
        };
    }
}
