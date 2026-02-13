using Bud.Shared.Domain;

namespace Bud.Shared.Contracts;

public sealed class UpdateCollaboratorTeamsRequest
{
    public List<Guid> TeamIds { get; set; } = new();
}

public sealed class UpdateTeamCollaboratorsRequest
{
    public List<Guid> CollaboratorIds { get; set; } = new();
}

public sealed class TeamSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string WorkspaceName { get; set; } = string.Empty;
}

public sealed class CollaboratorSummaryDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public CollaboratorRole Role { get; set; }
}
