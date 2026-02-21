namespace Bud.Server.Infrastructure.Querying;

public static class QuerySearchHelper
{
    public static IQueryable<T> ApplyCaseInsensitiveSearch<T>(
        IQueryable<T> query,
        string? search,
        bool isNpgsql,
        Func<IQueryable<T>, string, IQueryable<T>> npgsqlFilter,
        Func<IQueryable<T>, string, IQueryable<T>> fallbackFilter)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return query;
        }

        var term = search.Trim();
        if (isNpgsql)
        {
            var pattern = $"%{EscapeLikePattern(term)}%";
            return npgsqlFilter(query, pattern);
        }

        return fallbackFilter(query, term);
    }

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
