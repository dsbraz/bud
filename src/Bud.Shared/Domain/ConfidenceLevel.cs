namespace Bud.Shared.Domain;

public readonly record struct ConfidenceLevel
{
    private ConfidenceLevel(int value)
    {
        Value = value;
    }

    public int Value { get; }

    public static bool TryCreate(int raw, out ConfidenceLevel confidenceLevel)
    {
        confidenceLevel = default;
        if (raw < 1 || raw > 5)
        {
            return false;
        }

        confidenceLevel = new ConfidenceLevel(raw);
        return true;
    }

    public static ConfidenceLevel Create(int raw)
    {
        if (!TryCreate(raw, out var confidenceLevel))
        {
            throw new DomainInvariantException("Nível de confiança deve estar entre 1 e 5.");
        }

        return confidenceLevel;
    }
}
