using Bud.Shared.Domain;

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
    /// <summary>Nome da missão.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Descrição opcional da missão.</summary>
    public string? Description { get; set; }
    /// <summary>Data de início da missão.</summary>
    public DateTime StartDate { get; set; }
    /// <summary>Data de término da missão.</summary>
    public DateTime EndDate { get; set; }
    /// <summary>Status da missão.</summary>
    public MissionStatus Status { get; set; }
    /// <summary>Tipo de escopo da missão.</summary>
    public MissionScopeType ScopeType { get; set; }
    /// <summary>Identificador do escopo da missão.</summary>
    public Guid ScopeId { get; set; }
}

public sealed class UpdateMissionTemplateRequest
{
    /// <summary>Nome do template de missão.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Descrição opcional do template.</summary>
    public string? Description { get; set; }
    /// <summary>Padrão de nome para missões criadas a partir deste template.</summary>
    public string? MissionNamePattern { get; set; }
    /// <summary>Padrão de descrição para missões criadas a partir deste template.</summary>
    public string? MissionDescriptionPattern { get; set; }
    /// <summary>Indica se o template está ativo.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>Métricas do template.</summary>
    public List<MissionTemplateMetricDto> Metrics { get; set; } = [];
}

public sealed class UpdateMissionMetricRequest
{
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
