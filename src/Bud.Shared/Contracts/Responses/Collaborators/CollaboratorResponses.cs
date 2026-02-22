using Bud.Shared.Contracts;

namespace Bud.Shared.Contracts.Responses;

public sealed class OrganizationSummaryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class CollaboratorLookupResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public CollaboratorRole Role { get; set; }
}

public sealed class CollaboratorLeaderResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? TeamName { get; set; }
    public string? WorkspaceName { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
}

public sealed class CollaboratorSubordinateResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public List<CollaboratorSubordinateResponse> Children { get; set; } = [];
}

public sealed class CollaboratorTeamResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string WorkspaceName { get; set; } = string.Empty;
}

public sealed class TeamEligibleForAssignmentResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string WorkspaceName { get; set; } = string.Empty;
}

public sealed class CollaboratorEligibleForAssignmentResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public CollaboratorRole Role { get; set; }
}
