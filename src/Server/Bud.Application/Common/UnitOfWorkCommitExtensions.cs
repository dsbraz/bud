
namespace Bud.Application.Common;

internal static class UnitOfWorkCommitExtensions
{
    public static Task CommitAsync(
        this IUnitOfWork? unitOfWork,
        Func<CancellationToken, Task> fallback,
        CancellationToken cancellationToken = default)
    {
        return unitOfWork is null
            ? fallback(cancellationToken)
            : unitOfWork.CommitAsync(cancellationToken);
    }
}

