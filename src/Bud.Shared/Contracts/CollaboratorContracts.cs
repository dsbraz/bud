namespace Bud.Shared.Contracts;

public sealed class LeaderCollaboratorResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? TeamName { get; set; }
    public string? WorkspaceName { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
}

public sealed class CollaboratorHierarchyNodeDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public List<CollaboratorHierarchyNodeDto> Children { get; set; } = [];
}
