using Bud.Shared.Models;

namespace Bud.Shared.Contracts;

public sealed class AuthLoginRequest
{
    public string Email { get; set; } = string.Empty;
}

public sealed class AuthLoginResponse
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public Guid? CollaboratorId { get; set; }
    public CollaboratorRole? Role { get; set; }
    public Guid? OrganizationId { get; set; }
}

public sealed class OrganizationSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
