using Bud.Shared.Kernel;

namespace Bud.Shared.Contracts.Features.Teams;

public sealed class TeamResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid? ParentTeamId { get; set; }
    public Guid LeaderId { get; set; }
    public WorkspaceResponse? Workspace { get; set; }
    public TeamResponse? ParentTeam { get; set; }
    public List<TeamResponse> SubTeams { get; set; } = [];
    public List<CollaboratorResponse> Collaborators { get; set; } = [];
    public CollaboratorResponse? Leader { get; set; }
}

public sealed class TeamCollaboratorEligibleResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public CollaboratorRole Role { get; set; }
}
