using Bud.Application.Features.Organizations;
using Bud.Application.Features.Teams;

namespace Bud.Application.Features.Workspaces;

public static class WorkspaceContractMapper
{
    public static WorkspaceResponse ToResponse(this Workspace source)
    {
        return new WorkspaceResponse
        {
            Id = source.Id,
            Name = source.Name,
            OrganizationId = source.OrganizationId,
            Organization = source.Organization?.ToResponse(),
            Teams = source.Teams.Select(t => t.ToResponse()).ToList()
        };
    }

    // Returns a WorkspaceResponse without Teams, to break the Workspace↔Team cycle.
    public static WorkspaceResponse ToPartialResponse(this Workspace source)
    {
        return new WorkspaceResponse
        {
            Id = source.Id,
            Name = source.Name,
            OrganizationId = source.OrganizationId
        };
    }
}
