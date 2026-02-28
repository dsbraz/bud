using Bud.Server.Application.ReadModels;

namespace Bud.Server.Application.Mapping;

internal static class ProgressContractMapper
{
    public static GoalProgressResponse ToResponse(this GoalProgressSnapshot source)
    {
        return new GoalProgressResponse
        {
            GoalId = source.GoalId,
            OverallProgress = source.OverallProgress,
            ExpectedProgress = source.ExpectedProgress,
            AverageConfidence = source.AverageConfidence,
            TotalIndicators = source.TotalIndicators,
            IndicatorsWithCheckins = source.IndicatorsWithCheckins,
            OutdatedIndicators = source.OutdatedIndicators,
            DirectChildren = source.DirectChildren,
            DirectIndicators = source.DirectIndicators
        };
    }

    public static IndicatorProgressResponse ToResponse(this IndicatorProgressSnapshot source)
    {
        return new IndicatorProgressResponse
        {
            IndicatorId = source.IndicatorId,
            Progress = source.Progress,
            Confidence = source.Confidence,
            HasCheckins = source.HasCheckins,
            IsOutdated = source.IsOutdated,
            LastCheckinCollaboratorName = source.LastCheckinCollaboratorName
        };
    }
}
