namespace Bud.Shared.Contracts;

public sealed class LeaderCollaboratorResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string WorkspaceName { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
}
