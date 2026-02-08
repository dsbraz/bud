namespace Bud.Server.Domain.Common.Specifications;

public interface IQuerySpecification<T>
{
    IQueryable<T> Apply(IQueryable<T> query);
}
