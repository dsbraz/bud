namespace Bud.Shared.Domain;

public readonly record struct MetricRange
{
    private MetricRange(decimal minValue, decimal maxValue)
    {
        MinValue = minValue;
        MaxValue = maxValue;
    }

    public decimal MinValue { get; }
    public decimal MaxValue { get; }

    public static bool TryCreate(decimal? minValue, decimal? maxValue, out MetricRange range)
    {
        range = default;

        if (!minValue.HasValue || !maxValue.HasValue)
        {
            return false;
        }

        if (maxValue.Value <= minValue.Value)
        {
            return false;
        }

        range = new MetricRange(minValue.Value, maxValue.Value);
        return true;
    }

    public static MetricRange Create(decimal? minValue, decimal? maxValue)
    {
        if (!TryCreate(minValue, maxValue, out var range))
        {
            throw new DomainInvariantException("Valor máximo deve ser maior que o valor mínimo.");
        }

        return range;
    }
}
