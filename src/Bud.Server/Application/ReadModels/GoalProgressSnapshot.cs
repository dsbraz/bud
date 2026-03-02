namespace Bud.Server.Application.ReadModels;

public sealed class GoalProgressSnapshot
{
    public Guid GoalId { get; set; }
    public decimal OverallProgress { get; set; }
    public decimal ExpectedProgress { get; set; }
    public decimal AverageConfidence { get; set; }
    public int TotalIndicators { get; set; }
    public int IndicatorsWithCheckins { get; set; }
    public int OutdatedIndicators { get; set; }
    public int DirectChildren { get; set; }
    public int DirectIndicators { get; set; }
    public int TodoTasks { get; set; }
    public int DoingTasks { get; set; }
}
