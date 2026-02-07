namespace Bud.Shared.Contracts;

public sealed class CreateMetricCheckinRequest
{
    /// <summary>Identificador da métrica da missão.</summary>
    public Guid MissionMetricId { get; set; }
    /// <summary>Valor numérico do check-in (métricas quantitativas).</summary>
    public decimal? Value { get; set; }
    /// <summary>Texto do check-in (métricas qualitativas).</summary>
    public string? Text { get; set; }
    /// <summary>Data de referência do check-in.</summary>
    public DateTime CheckinDate { get; set; }
    /// <summary>Observação opcional do check-in.</summary>
    public string? Note { get; set; }
    /// <summary>Nível de confiança informado (1-5).</summary>
    public int ConfidenceLevel { get; set; }
}

public sealed class UpdateMetricCheckinRequest
{
    /// <summary>Valor numérico do check-in (métricas quantitativas).</summary>
    public decimal? Value { get; set; }
    /// <summary>Texto do check-in (métricas qualitativas).</summary>
    public string? Text { get; set; }
    /// <summary>Data de referência do check-in.</summary>
    public DateTime CheckinDate { get; set; }
    /// <summary>Observação opcional do check-in.</summary>
    public string? Note { get; set; }
    /// <summary>Nível de confiança informado (1-5).</summary>
    public int ConfidenceLevel { get; set; }
}
