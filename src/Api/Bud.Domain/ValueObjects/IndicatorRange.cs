namespace Bud.Domain.ValueObjects;

public readonly record struct IndicatorRange
{
    private IndicatorRange(decimal minValue, decimal maxValue)
    {
        MinValue = minValue;
        MaxValue = maxValue;
    }

    public decimal MinValue { get; }
    public decimal MaxValue { get; }

    public static bool TryCreate(decimal? minValue, decimal? maxValue, out IndicatorRange range)
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

        range = new IndicatorRange(minValue.Value, maxValue.Value);
        return true;
    }

    public static IndicatorRange Create(decimal? minValue, decimal? maxValue)
    {
        if (!TryCreate(minValue, maxValue, out var range))
        {
            throw new DomainInvariantException("Valor máximo deve ser maior que o valor mínimo.");
        }

        return range;
    }
}
