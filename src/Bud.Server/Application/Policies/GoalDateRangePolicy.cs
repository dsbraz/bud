using Bud.Server.Application.Common;

namespace Bud.Server.Application.Policies;

internal static class GoalDateRangePolicy
{
    public static Result<T>? ValidateChildStartDate<T>(
        DateTime childStartDate, DateTime parentStartDate)
    {
        if (childStartDate < parentStartDate)
        {
            return Result<T>.Failure(
                $"A data de início da meta não pode ser anterior à do pai ({parentStartDate:dd/MM/yyyy}).",
                ErrorType.Validation);
        }

        return null;
    }
}
