using System.Collections.Generic;
using System.Text.Json.Serialization;
using Bud.Shared.Contracts.Common.Json;

namespace Bud.Shared.Contracts.Common;

[JsonConverter(typeof(OptionalJsonConverterFactory))]
public readonly struct Optional<T> : IEquatable<Optional<T>>
{
    public bool HasValue { get; }
    public T? Value { get; }

    public Optional(T? value)
    {
        HasValue = true;
        Value = value;
    }

    public static implicit operator Optional<T>(T? value) => new(value);
    public static implicit operator T?(Optional<T> optional) => optional.Value;

    public bool Equals(Optional<T> other)
    {
        if (!HasValue && !other.HasValue)
        {
            return true;
        }

        if (HasValue != other.HasValue)
        {
            return false;
        }

        return EqualityComparer<T?>.Default.Equals(Value, other.Value);
    }

    public override bool Equals(object? obj) => obj is Optional<T> other && Equals(other);

    public override int GetHashCode()
    {
        if (!HasValue)
        {
            return 0;
        }

        return HashCode.Combine(1, Value);
    }

    public static bool operator ==(Optional<T> left, Optional<T> right) => left.Equals(right);

    public static bool operator !=(Optional<T> left, Optional<T> right) => !left.Equals(right);

    public static bool operator ==(Optional<T> left, T? right) =>
        left.HasValue && EqualityComparer<T?>.Default.Equals(left.Value, right);

    public static bool operator !=(Optional<T> left, T? right) => !(left == right);

    public static bool operator ==(T? left, Optional<T> right) => right == left;

    public static bool operator !=(T? left, Optional<T> right) => !(right == left);
}
