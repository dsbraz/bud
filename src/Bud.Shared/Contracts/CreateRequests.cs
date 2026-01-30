using Bud.Shared.Models;

namespace Bud.Shared.Contracts;

public sealed class CreateOrganizationRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
}

public sealed class CreateWorkspaceRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public Visibility? Visibility { get; set; }
}

public sealed class CreateTeamRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid WorkspaceId { get; set; }
    public Guid? ParentTeamId { get; set; }
}

public sealed class CreateCollaboratorRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public CollaboratorRole Role { get; set; } = CollaboratorRole.IndividualContributor;
    public Guid TeamId { get; set; }
}

public sealed class CreateMissionRequest
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public MissionStatus Status { get; set; }
    public MissionScopeType ScopeType { get; set; }
    public Guid ScopeId { get; set; }
}

public sealed class CreateMissionMetricRequest
{
    public Guid MissionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public MetricType Type { get; set; }
    public decimal? TargetValue { get; set; }
    public MetricUnit? Unit { get; set; }
    public string? TargetText { get; set; }
}
