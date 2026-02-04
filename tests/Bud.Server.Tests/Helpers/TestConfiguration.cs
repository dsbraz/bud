using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Bud.Server.Tests.Helpers;

public sealed class TestConfiguration : IConfiguration
{
    private readonly Dictionary<string, string> _data;

    public TestConfiguration(Dictionary<string, string>? data = null)
    {
        _data = data ?? new Dictionary<string, string>
        {
            ["Jwt:Key"] = "test-secret-key-for-unit-tests-minimum-32-characters",
            ["Jwt:Issuer"] = "bud-test",
            ["Jwt:Audience"] = "bud-test-api"
        };
    }

    public string? this[string key]
    {
        get => _data.TryGetValue(key, out var value) ? value : null;
        set => _data[key] = value ?? string.Empty;
    }

    public IConfigurationSection GetSection(string key)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<IConfigurationSection> GetChildren()
    {
        throw new NotImplementedException();
    }

    public IChangeToken GetReloadToken()
    {
        throw new NotImplementedException();
    }
}
