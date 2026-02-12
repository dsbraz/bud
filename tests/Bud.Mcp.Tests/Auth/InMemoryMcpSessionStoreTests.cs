namespace Bud.Mcp.Tests.Auth;

public sealed class InMemoryMcpSessionStoreTests
{
    [Fact]
    public async Task GetOrCreateAsync_WithoutSessionId_CreatesSession()
    {
        var options = new BudMcpOptions("http://bud.test", null, null, 30, 30);
        using var store = new InMemoryMcpSessionStore(options);

        var result = await store.GetOrCreateAsync(null);

        result.Created.Should().BeTrue();
        result.Context.SessionId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetOrCreateAsync_WithExistingSessionId_ReusesSession()
    {
        var options = new BudMcpOptions("http://bud.test", null, null, 30, 30);
        using var store = new InMemoryMcpSessionStore(options);

        var created = await store.GetOrCreateAsync(null);
        var reused = await store.GetOrCreateAsync(created.Context.SessionId);

        reused.Created.Should().BeFalse();
        reused.Context.Should().BeSameAs(created.Context);
    }

    [Fact]
    public async Task GetExistingAsync_WhenSessionExpires_ReturnsNull()
    {
        var options = new BudMcpOptions("http://bud.test", null, null, 30, 1);
        using var store = new InMemoryMcpSessionStore(options);

        var created = await store.GetOrCreateAsync(null);
        store.AdvanceClock(TimeSpan.FromMinutes(2));

        var existing = await store.GetExistingAsync(created.Context.SessionId);

        existing.Should().BeNull();
    }
}
