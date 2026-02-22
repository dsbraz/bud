using Bud.Shared.Contracts;

namespace Bud.Shared.Contracts.Requests;

public sealed class CreateOrganizationRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
}

public sealed class CreateWorkspaceRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
}

public sealed class CreateTeamRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid WorkspaceId { get; set; }
    public Guid LeaderId { get; set; }
    public Guid? ParentTeamId { get; set; }
}

public sealed class CreateCollaboratorRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public CollaboratorRole Role { get; set; } = CollaboratorRole.IndividualContributor;
    public Guid? TeamId { get; set; }
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

public sealed class CreateTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? MissionNamePattern { get; set; }
    public string? MissionDescriptionPattern { get; set; }
    public List<TemplateObjectiveRequest> Objectives { get; set; } = [];
    public List<TemplateMetricRequest> Metrics { get; set; } = [];
}

public sealed class CreateObjectiveRequest
{
    public Guid MissionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Dimension { get; set; }
}

public sealed class CreateMetricRequest
{
    public Guid MissionId { get; set; }
    public Guid? ObjectiveId { get; set; }
    public string Name { get; set; } = string.Empty;
    public MetricType Type { get; set; }
    public QuantitativeMetricType? QuantitativeType { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public MetricUnit? Unit { get; set; }
    public string? TargetText { get; set; }
}

public sealed class CreateCheckinRequest
{
    public decimal? Value { get; set; }
    public string? Text { get; set; }
    public DateTime CheckinDate { get; set; }
    public string? Note { get; set; }
    public int ConfidenceLevel { get; set; }
}
