namespace Bud.Server.Infrastructure.Querying;

public interface IQuerySpecification<T>
{
    IQueryable<T> Apply(IQueryable<T> query);
}
