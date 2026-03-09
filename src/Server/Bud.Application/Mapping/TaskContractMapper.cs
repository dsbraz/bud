using Bud.Domain.Model;

namespace Bud.Application.Mapping;

internal static class TaskContractMapper
{
    public static TaskResponse ToResponse(this GoalTask source)
    {
        return new TaskResponse
        {
            Id = source.Id,
            OrganizationId = source.OrganizationId,
            GoalId = source.GoalId,
            Name = source.Name,
            Description = source.Description,
            State = source.State,
            DueDate = source.DueDate
        };
    }
}
