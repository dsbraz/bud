namespace Bud.Shared.Contracts.Common;

public static class OptionalExtensions
{
    public static IEnumerable<T> AsEnumerable<T>(this Optional<IEnumerable<T>> source)
    {
        if (!source.HasValue || source.Value is null)
        {
            return [];
        }

        return source.Value;
    }

    public static IEnumerable<T> AsEnumerable<T>(this Optional<List<T>> source)
    {
        if (!source.HasValue || source.Value is null)
        {
            return [];
        }

        return source.Value;
    }
}
