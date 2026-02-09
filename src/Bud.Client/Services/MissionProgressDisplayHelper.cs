using Bud.Shared.Contracts;

namespace Bud.Client.Services;

public static class MissionProgressDisplayHelper
{
    public static string GetMissionProgressStatusClass(MissionProgressDto? progress)
    {
        if (progress is null || progress.MetricsWithCheckins == 0)
        {
            return "no-data";
        }

        if (progress.ExpectedProgress <= 0 || progress.OverallProgress >= progress.ExpectedProgress)
        {
            return "on-track";
        }

        return progress.OverallProgress >= progress.ExpectedProgress * 0.7m ? "at-risk" : "off-track";
    }

    public static string GetMetricProgressStatusClass(MetricProgressDto progress)
    {
        if (!progress.HasCheckins)
        {
            return "no-data";
        }

        return progress.Progress switch
        {
            >= 70m => "on-track",
            >= 40m => "at-risk",
            _ => "off-track"
        };
    }

    public static (string statusClass, string label) GetConfidenceDisplay(int confidence)
    {
        return confidence switch
        {
            >= 4 => ("on-track", "No caminho"),
            >= 2 => ("at-risk", "Atenção"),
            _ => ("off-track", "Em risco")
        };
    }

    public static (string statusClass, string label) GetConfidenceDisplay(decimal averageConfidence)
    {
        return averageConfidence switch
        {
            >= 4m => ("on-track", "No caminho"),
            >= 2m => ("at-risk", "Atenção"),
            _ => ("off-track", "Em risco")
        };
    }

    public static string GetConfidenceClass(decimal confidence)
    {
        return confidence switch
        {
            >= 4m => "high",
            >= 2.5m => "medium",
            _ => "low"
        };
    }

    public static string GetConfidenceLabel(decimal confidence)
    {
        return confidence switch
        {
            >= 4m => "Alta",
            >= 2.5m => "Média",
            _ => "Baixa"
        };
    }

    public static string GetConfidenceStarsText(int confidence)
    {
        var full = Math.Clamp(confidence, 0, 5);
        return new string('\u2605', full) + new string('\u2606', 5 - full);
    }

    public static string GetConfidenceStarsText(decimal confidence)
    {
        var full = (int)Math.Round(confidence);
        return new string('★', full) + new string('☆', 5 - full);
    }
}
