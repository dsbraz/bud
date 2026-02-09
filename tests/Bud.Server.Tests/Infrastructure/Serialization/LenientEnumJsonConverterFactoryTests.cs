using System.Text.Json;
using Bud.Server.Infrastructure.Serialization;
using Bud.Shared.Models;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Infrastructure.Serialization;

public sealed class LenientEnumJsonConverterFactoryTests
{
    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new LenientEnumJsonConverterFactory());
        return options;
    }

    [Fact]
    public void Deserialize_StringEnum_ShouldParseIgnoringCase()
    {
        var json = """{"status":"planned"}""";

        var result = JsonSerializer.Deserialize<TestEnumPayload>(json, CreateOptions());

        result.Should().NotBeNull();
        result!.Status.Should().Be(MissionStatus.Planned);
    }

    [Fact]
    public void Deserialize_NumericEnum_ShouldParse()
    {
        var json = """{"status":0}""";

        var result = JsonSerializer.Deserialize<TestEnumPayload>(json, CreateOptions());

        result.Should().NotBeNull();
        result!.Status.Should().Be(MissionStatus.Planned);
    }

    [Fact]
    public void Deserialize_NullableEnumNull_ShouldRemainNull()
    {
        var json = """{"quantitativeType":null}""";

        var result = JsonSerializer.Deserialize<TestNullableEnumPayload>(json, CreateOptions());

        result.Should().NotBeNull();
        result!.QuantitativeType.Should().BeNull();
    }

    [Fact]
    public void Deserialize_InvalidEnum_ShouldThrowJsonException()
    {
        var json = """{"status":"unknown"}""";

        var action = () => JsonSerializer.Deserialize<TestEnumPayload>(json, CreateOptions());

        action.Should().Throw<JsonException>();
    }

    private sealed class TestEnumPayload
    {
        public MissionStatus Status { get; init; }
    }

    private sealed class TestNullableEnumPayload
    {
        public QuantitativeMetricType? QuantitativeType { get; init; }
    }
}
