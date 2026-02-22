namespace Bud.Server.Application.Mapping;

internal static class PagingContractMapper
{
    public static PagedResult<TDestination> MapPaged<TSource, TDestination>(
        this PagedResult<TSource> source,
        Func<TSource, TDestination> map)
    {
        return new PagedResult<TDestination>
        {
            Items = source.Items.Select(map).ToList(),
            Total = source.Total,
            Page = source.Page,
            PageSize = source.PageSize
        };
    }
}
