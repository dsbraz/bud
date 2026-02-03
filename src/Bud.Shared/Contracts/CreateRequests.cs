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
    public Guid? LeaderId { get; set; }
}

public sealed class CreateMissionRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
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

    // Quantitative metric fields
    public QuantitativeMetricType? QuantitativeType { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public MetricUnit? Unit { get; set; }

    // Qualitative metric fields
    public string? TargetText { get; set; }
}
