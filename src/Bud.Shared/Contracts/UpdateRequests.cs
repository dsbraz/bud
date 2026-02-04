using Bud.Shared.Models;

namespace Bud.Shared.Contracts;

public sealed class UpdateOrganizationRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid? OwnerId { get; set; }
}

public sealed class UpdateWorkspaceRequest
{
    public string Name { get; set; } = string.Empty;
}

public sealed class UpdateTeamRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid? ParentTeamId { get; set; }
}

public sealed class UpdateCollaboratorRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public CollaboratorRole Role { get; set; } = CollaboratorRole.IndividualContributor;
    public Guid? LeaderId { get; set; }
}

public sealed class UpdateMissionRequest
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public MissionStatus Status { get; set; }
}

public sealed class UpdateMissionMetricRequest
{
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
