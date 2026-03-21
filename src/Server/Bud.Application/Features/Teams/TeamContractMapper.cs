using Bud.Application.Features.Collaborators;
using Bud.Application.Features.Workspaces;

namespace Bud.Application.Features.Teams;

public static class TeamContractMapper
{
    public static TeamResponse ToResponse(this Team source)
    {
        return new TeamResponse
        {
            Id = source.Id,
            Name = source.Name,
            OrganizationId = source.OrganizationId,
            WorkspaceId = source.WorkspaceId,
            ParentTeamId = source.ParentTeamId,
            LeaderId = source.LeaderId,
            Workspace = source.Workspace?.ToPartialResponse(),
            ParentTeam = source.ParentTeam?.ToResponse(),
            SubTeams = source.SubTeams.Select(t => t.ToResponse()).ToList(),
            Collaborators = source.Collaborators.Select(c => c.ToCollaboratorResponse()).ToList(),
            Leader = source.Leader?.ToCollaboratorResponse()
        };
    }
}
