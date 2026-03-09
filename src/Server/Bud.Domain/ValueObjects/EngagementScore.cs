namespace Bud.Domain.ValueObjects;

public readonly record struct EngagementScore
{
    private EngagementScore(int score, string level, string tip)
    {
        Score = score;
        Level = level;
        Tip = tip;
    }

    public int Score { get; }
    public string Level { get; }
    public string Tip { get; }

    public static EngagementScore Create(int score)
    {
        if (score < 0 || score > 100)
        {
            throw new DomainInvariantException("Score de engajamento deve estar entre 0 e 100.");
        }

        var level = score >= 70 ? "high" : score >= 40 ? "medium" : "low";

        var tip = level switch
        {
            "high" => "Excelente! Seu time está engajado e acompanhando as missões de perto.",
            "medium" => "Bom progresso! Incentive o time a manter a frequência de check-ins.",
            _ => "Atenção: o engajamento está baixo. Considere alinhar prioridades com o time."
        };

        return new EngagementScore(score, level, tip);
    }

    public static EngagementScore Zero() => new(0, "low", "Atenção: o engajamento está baixo. Considere alinhar prioridades com o time.");
}
