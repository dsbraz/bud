using Bud.Shared.Models;

namespace Bud.Shared.Contracts;

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
    /// <summary>Nome da missão.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Descrição opcional da missão.</summary>
    public string? Description { get; set; }
    /// <summary>Data de início da missão.</summary>
    public DateTime StartDate { get; set; }
    /// <summary>Data de término da missão.</summary>
    public DateTime EndDate { get; set; }
    /// <summary>Status inicial da missão.</summary>
    public MissionStatus Status { get; set; }
    /// <summary>Tipo de escopo da missão (Organização, Workspace, Time ou Colaborador).</summary>
    public MissionScopeType ScopeType { get; set; }
    /// <summary>Identificador do escopo da missão.</summary>
    public Guid ScopeId { get; set; }
}

public sealed class CreateMissionMetricRequest
{
    /// <summary>Identificador da missão.</summary>
    public Guid MissionId { get; set; }
    /// <summary>Nome da métrica.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Tipo da métrica (quantitativa ou qualitativa).</summary>
    public MetricType Type { get; set; }

    // Quantitative metric fields
    /// <summary>Tipo de métrica quantitativa quando aplicável.</summary>
    public QuantitativeMetricType? QuantitativeType { get; set; }
    /// <summary>Valor mínimo esperado quando aplicável.</summary>
    public decimal? MinValue { get; set; }
    /// <summary>Valor máximo esperado quando aplicável.</summary>
    public decimal? MaxValue { get; set; }
    /// <summary>Unidade da métrica quantitativa quando aplicável.</summary>
    public MetricUnit? Unit { get; set; }

    // Qualitative metric fields
    /// <summary>Texto-alvo para métricas qualitativas.</summary>
    public string? TargetText { get; set; }
}
