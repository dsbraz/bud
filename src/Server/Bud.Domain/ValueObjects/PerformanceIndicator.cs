namespace Bud.Domain.ValueObjects;

public readonly record struct PerformanceIndicator
{
    private PerformanceIndicator(int percentage, int deltaPercentage, bool isPlaceholder)
    {
        Percentage = percentage;
        DeltaPercentage = deltaPercentage;
        IsPlaceholder = isPlaceholder;
    }

    public int Percentage { get; }
    public int DeltaPercentage { get; }
    public bool IsPlaceholder { get; }

    public static PerformanceIndicator Create(int percentage, int deltaPercentage, bool isPlaceholder = false)
    {
        if (percentage < 0 || percentage > 100)
        {
            throw new DomainInvariantException("Percentual do indicador deve estar entre 0 e 100.");
        }

        if (deltaPercentage < -100 || deltaPercentage > 100)
        {
            throw new DomainInvariantException("Variação do indicador deve estar entre -100 e 100.");
        }

        return new PerformanceIndicator(percentage, deltaPercentage, isPlaceholder);
    }

    public static PerformanceIndicator Placeholder() => new(0, 0, true);

    public static PerformanceIndicator Zero() => new(0, 0, false);
}
