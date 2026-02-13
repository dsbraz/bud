using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bud.Server.Infrastructure;

public sealed class LenientEnumJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        var enumType = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;
        return enumType.IsEnum;
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var nullableEnumType = Nullable.GetUnderlyingType(typeToConvert);
        var converterType = nullableEnumType is null
            ? typeof(LenientEnumJsonConverter<>).MakeGenericType(typeToConvert)
            : typeof(LenientNullableEnumJsonConverter<>).MakeGenericType(nullableEnumType);

        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private sealed class LenientEnumJsonConverter<TEnum> : JsonConverter<TEnum>
        where TEnum : struct, Enum
    {
        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return ReadEnumValue<TEnum>(ref reader, typeof(TEnum).Name);
        }

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(Convert.ToInt64(value, CultureInfo.InvariantCulture));
        }
    }

    private sealed class LenientNullableEnumJsonConverter<TEnum> : JsonConverter<TEnum?>
        where TEnum : struct, Enum
    {
        public override TEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            return ReadEnumValue<TEnum>(ref reader, $"{typeof(TEnum).Name}?");
        }

        public override void Write(Utf8JsonWriter writer, TEnum? value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteNumberValue(Convert.ToInt64(value.Value, CultureInfo.InvariantCulture));
        }
    }

    private static TEnum ReadEnumValue<TEnum>(ref Utf8JsonReader reader, string enumDisplayName)
        where TEnum : struct, Enum
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var raw = reader.GetString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                throw new JsonException($"Valor de enum inválido para {enumDisplayName}.");
            }

            if (Enum.TryParse<TEnum>(raw, ignoreCase: true, out var parsedFromString))
            {
                return parsedFromString;
            }

            throw new JsonException($"Valor de enum inválido para {enumDisplayName}: {raw}.");
        }

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out var longValue))
        {
            var enumValue = (TEnum)Enum.ToObject(typeof(TEnum), longValue);
            if (Enum.IsDefined(enumValue))
            {
                return enumValue;
            }

            throw new JsonException($"Valor de enum inválido para {enumDisplayName}: {longValue}.");
        }

        throw new JsonException($"Token inválido para enum {enumDisplayName}.");
    }
}
