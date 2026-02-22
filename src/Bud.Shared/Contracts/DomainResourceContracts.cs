using Bud.Shared.Contracts;

namespace Bud.Shared.Contracts.Responses;

public sealed class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? OwnerId { get; set; }
    public Collaborator? Owner { get; set; }
}

public sealed class Workspace
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public List<Team> Teams { get; set; } = [];
}

public sealed class Team
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid? ParentTeamId { get; set; }
    public Guid LeaderId { get; set; }
    public Workspace? Workspace { get; set; }
    public Team? ParentTeam { get; set; }
    public List<Team> SubTeams { get; set; } = [];
    public List<Collaborator> Collaborators { get; set; } = [];
    public Collaborator? Leader { get; set; }
}

public sealed class Collaborator
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public CollaboratorRole Role { get; set; } = CollaboratorRole.IndividualContributor;
    public Guid OrganizationId { get; set; }
    public Guid? TeamId { get; set; }
    public Guid? LeaderId { get; set; }
    public bool IsGlobalAdmin { get; set; }
    public Team? Team { get; set; }
    public Collaborator? Leader { get; set; }
}

public sealed class Mission
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public MissionStatus Status { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? WorkspaceId { get; set; }
    public Guid? TeamId { get; set; }
    public Guid? CollaboratorId { get; set; }
    public Workspace? Workspace { get; set; }
    public Team? Team { get; set; }
    public Collaborator? Collaborator { get; set; }
    public List<Metric> Metrics { get; set; } = [];
    public List<Objective> Objectives { get; set; } = [];
}

public sealed class Objective
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid MissionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Dimension { get; set; }
    public List<Metric> Metrics { get; set; } = [];
}

public sealed class Metric
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid MissionId { get; set; }
    public Guid? ObjectiveId { get; set; }
    public string Name { get; set; } = string.Empty;
    public MetricType Type { get; set; }
    public QuantitativeMetricType? QuantitativeType { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public MetricUnit? Unit { get; set; }
    public string? TargetText { get; set; }
    public Objective? MissionObjective { get; set; }
    public List<MetricCheckin> Checkins { get; set; } = [];
}

public sealed class MetricCheckin
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid MetricId { get; set; }
    public Guid CollaboratorId { get; set; }
    public decimal? Value { get; set; }
    public string? Text { get; set; }
    public DateTime CheckinDate { get; set; }
    public string? Note { get; set; }
    public int ConfidenceLevel { get; set; }
    public Collaborator? Collaborator { get; set; }
}

public sealed class MissionTemplate
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? MissionNamePattern { get; set; }
    public string? MissionDescriptionPattern { get; set; }
    public List<MissionTemplateObjective> Objectives { get; set; } = [];
    public List<MissionTemplateMetric> Metrics { get; set; } = [];
}

public sealed class MissionTemplateObjective
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid MissionTemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
    public string? Dimension { get; set; }
    public List<MissionTemplateMetric> Metrics { get; set; } = [];
}

public sealed class MissionTemplateMetric
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid MissionTemplateId { get; set; }
    public Guid? MissionTemplateObjectiveId { get; set; }
    public string Name { get; set; } = string.Empty;
    public MetricType Type { get; set; }
    public int OrderIndex { get; set; }
    public QuantitativeMetricType? QuantitativeType { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public MetricUnit? Unit { get; set; }
    public string? TargetText { get; set; }
    public MissionTemplateObjective? MissionTemplateObjective { get; set; }
}
