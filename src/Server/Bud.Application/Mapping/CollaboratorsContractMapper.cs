using Bud.Domain.Model;

namespace Bud.Application.Mapping;

internal static class CollaboratorsContractMapper
{
    public static CollaboratorLookupResponse ToResponse(this Collaborator source)
    {
        return new CollaboratorLookupResponse
        {
            Id = source.Id,
            FullName = source.FullName,
            Email = source.Email,
            Role = source.Role        };
    }

    public static CollaboratorTeamResponse ToResponse(this Team source)
    {
        return new CollaboratorTeamResponse
        {
            Id = source.Id,
            Name = source.Name,
            WorkspaceName = source.Workspace?.Name ?? string.Empty
        };
    }

    public static CollaboratorTeamEligibleResponse ToCollaboratorTeamEligibleResponse(this Team source)
    {
        return new CollaboratorTeamEligibleResponse
        {
            Id = source.Id,
            Name = source.Name,
            WorkspaceName = source.Workspace?.Name ?? string.Empty
        };
    }

    public static TeamCollaboratorEligibleResponse ToTeamCollaboratorEligibleResponse(this Collaborator source)
    {
        return new TeamCollaboratorEligibleResponse
        {
            Id = source.Id,
            FullName = source.FullName,
            Email = source.Email,
            Role = source.Role        };
    }

    public static CollaboratorLeaderResponse ToLeaderResponse(this Collaborator source)
    {
        return new CollaboratorLeaderResponse
        {
            Id = source.Id,
            FullName = source.FullName,
            Email = source.Email,
            TeamName = source.Team?.Name,
            WorkspaceName = source.Team?.Workspace?.Name,
            OrganizationName = source.Organization?.Name ?? string.Empty
        };
    }
}
