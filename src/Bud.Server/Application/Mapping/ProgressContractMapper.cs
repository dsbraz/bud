using Bud.Server.Domain.ReadModels;

namespace Bud.Server.Application.Mapping;

internal static class ProgressContractMapper
{
    public static MissionProgressResponse ToResponse(this MissionProgressSnapshot source)
    {
        return new MissionProgressResponse
        {
            MissionId = source.MissionId,
            OverallProgress = source.OverallProgress,
            ExpectedProgress = source.ExpectedProgress,
            AverageConfidence = source.AverageConfidence,
            TotalMetrics = source.TotalMetrics,
            MetricsWithCheckins = source.MetricsWithCheckins,
            OutdatedMetrics = source.OutdatedMetrics,
            ObjectiveProgress = source.ObjectiveProgress.Select(ToResponse).ToList()
        };
    }

    public static ObjectiveProgressResponse ToResponse(this ObjectiveProgressSnapshot source)
    {
        return new ObjectiveProgressResponse
        {
            ObjectiveId = source.ObjectiveId,
            OverallProgress = source.OverallProgress,
            AverageConfidence = source.AverageConfidence,
            TotalMetrics = source.TotalMetrics,
            MetricsWithCheckins = source.MetricsWithCheckins,
            OutdatedMetrics = source.OutdatedMetrics
        };
    }

    public static MetricProgressResponse ToResponse(this MetricProgressSnapshot source)
    {
        return new MetricProgressResponse
        {
            MetricId = source.MetricId,
            Progress = source.Progress,
            Confidence = source.Confidence,
            HasCheckins = source.HasCheckins,
            IsOutdated = source.IsOutdated,
            LastCheckinCollaboratorName = source.LastCheckinCollaboratorName
        };
    }
}
