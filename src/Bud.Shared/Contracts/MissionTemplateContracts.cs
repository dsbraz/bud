
using Bud.Shared.Contracts;

namespace Bud.Shared.Contracts;

public sealed class MissionTemplateMetricDto
{
    /// <summary>Identificador opcional do objetivo do template ao qual a métrica pertence.</summary>
    public Guid? MissionTemplateObjectiveId { get; set; }
    /// <summary>Nome da métrica do template.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Tipo da métrica (quantitativa ou qualitativa).</summary>
    public MetricType Type { get; set; }
    /// <summary>Índice de ordenação da métrica no template.</summary>
    public int OrderIndex { get; set; }

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

public sealed class MissionTemplateObjectiveDto
{
    /// <summary>Identificador opcional do objetivo no template.</summary>
    public Guid? Id { get; set; }
    /// <summary>Nome do objetivo do template.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Descrição opcional do objetivo do template.</summary>
    public string? Description { get; set; }
    /// <summary>Índice de ordenação do objetivo no template.</summary>
    public int OrderIndex { get; set; }
    /// <summary>Identificador opcional da dimensão do objetivo.</summary>
    public string? Dimension { get; set; }
}
