namespace Bud.Server.Domain.Specifications;

public interface IQuerySpecification<T>
{
    IQueryable<T> Apply(IQueryable<T> query);
}
