using Bud.Shared.Contracts;

namespace Bud.Shared.Contracts.Responses;

public sealed class SessionResponse
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsGlobalAdmin { get; set; }
    public Guid? CollaboratorId { get; set; }
    public CollaboratorRole? Role { get; set; }
    public Guid? OrganizationId { get; set; }
}
