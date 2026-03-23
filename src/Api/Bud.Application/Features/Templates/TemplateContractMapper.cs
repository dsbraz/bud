namespace Bud.Application.Features.Templates;

public static class TemplateContractMapper
{
    public static TemplateResponse ToResponse(this Template source)
    {
        return new TemplateResponse
        {
            Id = source.Id,
            OrganizationId = source.OrganizationId,
            Name = source.Name,
            Description = source.Description,
            GoalNamePattern = source.GoalNamePattern,
            GoalDescriptionPattern = source.GoalDescriptionPattern,
            Goals = source.Goals.Select(g => g.ToResponse(source.Indicators)).ToList(),
            Indicators = source.Indicators.Select(i => i.ToResponse()).ToList()
        };
    }

    public static TemplateGoalResponse ToResponse(this TemplateGoal source, IEnumerable<TemplateIndicator> allIndicators)
    {
        return new TemplateGoalResponse
        {
            Id = source.Id,
            OrganizationId = source.OrganizationId,
            TemplateId = source.TemplateId,
            ParentId = source.ParentId,
            Name = source.Name,
            Description = source.Description,
            OrderIndex = source.OrderIndex,
            Dimension = source.Dimension,
            Indicators = allIndicators
                .Where(i => i.TemplateGoalId == source.Id)
                .Select(i => i.ToResponse())
                .ToList()
        };
    }

    public static TemplateIndicatorResponse ToResponse(this TemplateIndicator source)
    {
        return new TemplateIndicatorResponse
        {
            Id = source.Id,
            OrganizationId = source.OrganizationId,
            TemplateId = source.TemplateId,
            TemplateGoalId = source.TemplateGoalId,
            Name = source.Name,
            Type = source.Type,
            OrderIndex = source.OrderIndex,
            QuantitativeType = source.QuantitativeType,
            MinValue = source.MinValue,
            MaxValue = source.MaxValue,
            Unit = source.Unit,
            TargetText = source.TargetText
        };
    }
}
