using Bud.Shared.Contracts.Common;
using FluentValidation;

namespace Bud.Server.Validators;

internal static class OptionalValidationExtensions
{
    public static IRuleBuilderOptions<T, Optional<string>> MaximumLength<T>(
        this IRuleBuilder<T, Optional<string>> ruleBuilder,
        int max)
    {
        return ruleBuilder.Must(value => !value.HasValue || value.Value is null || value.Value.Length <= max);
    }

    public static IRuleBuilderOptions<T, Optional<int>> InclusiveBetween<T>(
        this IRuleBuilder<T, Optional<int>> ruleBuilder,
        int from,
        int to)
    {
        return ruleBuilder.Must(value => !value.HasValue || (value.Value >= from && value.Value <= to));
    }

    public static IRuleBuilderOptions<T, Optional<decimal?>> GreaterThanOrEqualTo<T>(
        this IRuleBuilder<T, Optional<decimal?>> ruleBuilder,
        decimal min)
    {
        return ruleBuilder.Must(value => !value.HasValue || !value.Value.HasValue || value.Value.Value >= min);
    }
}
