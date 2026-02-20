namespace Bud.Server.Application.Common;

public static class PaginationNormalizer
{
    public static (int page, int pageSize) Normalize(
        int page,
        int pageSize,
        int defaultPageSize = 10,
        int maxPageSize = 100)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1 || pageSize > maxPageSize ? defaultPageSize : pageSize;
        return (normalizedPage, normalizedPageSize);
    }
}
