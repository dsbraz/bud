namespace Bud.Server.Domain.Primitives;

public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}

