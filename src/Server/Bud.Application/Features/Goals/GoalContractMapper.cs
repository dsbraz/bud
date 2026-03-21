using Bud.Application.Features.Collaborators;
using Bud.Application.Features.Indicators;
using Bud.Application.Features.Tasks;

namespace Bud.Application.Features.Goals;

public static class GoalContractMapper
{
    public static GoalResponse ToResponse(this Goal source)
    {
        return new GoalResponse
        {
            Id = source.Id,
            Name = source.Name,
            Description = source.Description,
            Dimension = source.Dimension,
            StartDate = source.StartDate,
            EndDate = source.EndDate,
            Status = source.Status,
            OrganizationId = source.OrganizationId,
            ParentId = source.ParentId,
            CollaboratorId = source.CollaboratorId,
            Collaborator = source.Collaborator?.ToCollaboratorResponse(),
            Children = source.Children.Select(c => c.ToResponse()).ToList(),
            Indicators = source.Indicators.Select(i => i.ToResponse()).ToList(),
            Tasks = source.Tasks.Select(t => t.ToResponse()).ToList()
        };
    }
}
