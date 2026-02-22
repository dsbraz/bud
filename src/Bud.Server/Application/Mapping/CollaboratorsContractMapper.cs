using Bud.Server.Domain.ReadModels;
using Bud.Server.Domain.Model;

namespace Bud.Server.Application.Mapping;

internal static class CollaboratorsContractMapper
{
    public static CollaboratorLookupResponse ToResponse(this CollaboratorSummary source)
    {
        return new CollaboratorLookupResponse
        {
            Id = source.Id,
            FullName = source.FullName,
            Email = source.Email,
            Role = source.Role.ToShared()
        };
    }

    public static CollaboratorLookupResponse ToResponse(this Collaborator source)
    {
        return new CollaboratorLookupResponse
        {
            Id = source.Id,
            FullName = source.FullName,
            Email = source.Email,
            Role = source.Role.ToShared()
        };
    }

    public static CollaboratorTeamResponse ToResponse(this TeamSummary source)
    {
        return new CollaboratorTeamResponse
        {
            Id = source.Id,
            Name = source.Name,
            WorkspaceName = source.WorkspaceName
        };
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

    public static TeamEligibleForAssignmentResponse ToTeamEligibleForAssignmentResponse(this Team source)
    {
        return new TeamEligibleForAssignmentResponse
        {
            Id = source.Id,
            Name = source.Name,
            WorkspaceName = source.Workspace?.Name ?? string.Empty
        };
    }

    public static CollaboratorEligibleForAssignmentResponse ToCollaboratorEligibleForAssignmentResponse(this Collaborator source)
    {
        return new CollaboratorEligibleForAssignmentResponse
        {
            Id = source.Id,
            FullName = source.FullName,
            Email = source.Email,
            Role = source.Role.ToShared()
        };
    }

    public static CollaboratorLeaderResponse ToResponse(this LeaderCollaborator source)
    {
        return new CollaboratorLeaderResponse
        {
            Id = source.Id,
            FullName = source.FullName,
            Email = source.Email,
            TeamName = source.TeamName,
            WorkspaceName = source.WorkspaceName,
            OrganizationName = source.OrganizationName
        };
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

    public static CollaboratorSubordinateResponse ToResponse(this CollaboratorHierarchyNode source)
    {
        return new CollaboratorSubordinateResponse
        {
            Id = source.Id,
            FullName = source.FullName,
            Initials = source.Initials,
            Role = source.Role,
            Children = source.Children.Select(ToResponse).ToList()
        };
    }
}
