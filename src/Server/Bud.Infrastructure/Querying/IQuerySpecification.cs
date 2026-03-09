namespace Bud.Infrastructure.Querying;

public interface IQuerySpecification<T>
{
    IQueryable<T> Apply(IQueryable<T> query);
}
