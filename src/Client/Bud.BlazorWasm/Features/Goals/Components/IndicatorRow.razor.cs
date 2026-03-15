using Bud.Shared.Contracts.Features.Goals;
using Microsoft.AspNetCore.Components;

namespace Bud.BlazorWasm.Features.Goals.Components;

public partial class IndicatorRow
{
    [Parameter, EditorRequired] public GoalResponse Goal { get; set; } = default!;
    [Parameter, EditorRequired] public IndicatorResponse Indicator { get; set; } = default!;
    [Parameter] public GoalProgressResponse? GoalProgressResponse { get; set; }
    [Parameter] public IndicatorProgressResponse? IndicatorProgressResponse { get; set; }
    [Parameter] public EventCallback<IndicatorResponse> OnCheckinClick { get; set; }
    [Parameter] public EventCallback<IndicatorResponse> OnHistoryClick { get; set; }

    private int MetricProgressPercent => (int)(IndicatorProgressResponse?.Progress ?? 0);
    private decimal ExpectedProgress => GoalProgressResponse?.ExpectedProgress ?? 0m;
    private bool IsMetricOutdated => IndicatorProgressResponse?.IsOutdated ?? true;

    private string MetricStatusClass => GetMetricStatusClass(MetricProgressPercent, ExpectedProgress);
    private string MetricStatusLabel => GetMetricStatusLabel(MetricProgressPercent, ExpectedProgress, Indicator);

    private bool HasLastCheckinAuthor => IndicatorProgressResponse?.LastCheckinCollaboratorName is not null;
    private string LastCheckinAuthorName => IndicatorProgressResponse?.LastCheckinCollaboratorName ?? "Último check-in";

    private string GetQuarterLabel()
    {
        var quarter = (Goal.StartDate.Month - 1) / 3 + 1;
        return $"Q{quarter} {Goal.StartDate.Year}";
    }

    private static string GetMetricStatusClass(int progress, decimal expectedProgress)
    {
        if (expectedProgress <= 0)
        {
            return "on-track";
        }

        if (progress >= (int)expectedProgress)
        {
            return "on-track";
        }

        if (progress >= (int)(expectedProgress * 0.7m))
        {
            return "at-risk";
        }

        return "off-track";
    }

    private static string GetMetricStatusLabel(int progress, decimal expectedProgress, IndicatorResponse indicator)
    {
        if (indicator.Type == IndicatorType.Qualitative)
        {
            return "Qualitativo";
        }

        if (expectedProgress <= 0)
        {
            return "Dentro do previsto";
        }

        if (progress >= (int)expectedProgress)
        {
            return "Dentro do previsto";
        }

        if (progress >= (int)(expectedProgress * 0.7m))
        {
            return "Atenção";
        }

        return "Em risco";
    }

    private static string GetInitials(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return "?";
        }

        if (parts.Length == 1)
        {
            return parts[0][..1].ToUpperInvariant();
        }

        return $"{parts[0][..1]}{parts[^1][..1]}".ToUpperInvariant();
    }

    private RenderFragment GetTypeIconSvg() => Indicator.Type == IndicatorType.Quantitative && Indicator.QuantitativeType.HasValue
        ? Indicator.QuantitativeType.Value switch
        {
            QuantitativeIndicatorType.Achieve => TrophyIcon,
            QuantitativeIndicatorType.KeepBetween => ArrowsInLineVerticalIcon,
            QuantitativeIndicatorType.KeepAbove => PlugsConnectedIcon,
            QuantitativeIndicatorType.KeepBelow => ArrowDownIcon,
            QuantitativeIndicatorType.Reduce => ArrowDownIcon,
            _ => TrophyIcon
        }
        : QualitativeIcon;
}
