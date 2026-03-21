using Bud.Application.Features.Collaborators;

namespace Bud.Application.Features.Indicators;

public static class IndicatorContractMapper
{
    public static IndicatorResponse ToResponse(this Indicator source)
    {
        return new IndicatorResponse
        {
            Id = source.Id,
            OrganizationId = source.OrganizationId,
            GoalId = source.GoalId,
            Name = source.Name,
            Type = source.Type,
            QuantitativeType = source.QuantitativeType,
            MinValue = source.MinValue,
            MaxValue = source.MaxValue,
            Unit = source.Unit,
            TargetText = source.TargetText,
            Checkins = source.Checkins.Select(c => c.ToResponse()).ToList()
        };
    }

    public static CheckinResponse ToResponse(this Checkin source)
    {
        return new CheckinResponse
        {
            Id = source.Id,
            OrganizationId = source.OrganizationId,
            IndicatorId = source.IndicatorId,
            CollaboratorId = source.CollaboratorId,
            Value = source.Value,
            Text = source.Text,
            CheckinDate = source.CheckinDate,
            Note = source.Note,
            ConfidenceLevel = source.ConfidenceLevel,
            Collaborator = source.Collaborator?.ToCollaboratorResponse()
        };
    }
}
