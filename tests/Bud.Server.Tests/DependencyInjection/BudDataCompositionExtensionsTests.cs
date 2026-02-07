using Bud.Server.DependencyInjection;
using Bud.Server.Infrastructure.Events;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bud.Server.Tests.DependencyInjection;

public sealed class BudDataCompositionExtensionsTests
{
    [Fact]
    public void AddBudDataAccess_ShouldBindOutboxHealthCheckOptions_FromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=bud;Username=postgres;Password=postgres",
                ["Outbox:HealthCheck:MaxDeadLetters"] = "7",
                ["Outbox:HealthCheck:MaxOldestPendingAge"] = "00:02:30"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddBudDataAccess(configuration);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<OutboxHealthCheckOptions>>().Value;
        options.MaxDeadLetters.Should().Be(7);
        options.MaxOldestPendingAge.Should().Be(TimeSpan.FromMinutes(2.5));
    }

    [Fact]
    public void AddBudDataAccess_ShouldBindOutboxProcessingOptions_FromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=bud;Username=postgres;Password=postgres",
                ["Outbox:Processing:MaxRetries"] = "8",
                ["Outbox:Processing:BaseRetryDelay"] = "00:00:03",
                ["Outbox:Processing:MaxRetryDelay"] = "00:04:00",
                ["Outbox:Processing:BatchSize"] = "50",
                ["Outbox:Processing:PollingInterval"] = "00:00:07"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddBudDataAccess(configuration);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<OutboxProcessingOptions>>().Value;
        options.MaxRetries.Should().Be(8);
        options.BaseRetryDelay.Should().Be(TimeSpan.FromSeconds(3));
        options.MaxRetryDelay.Should().Be(TimeSpan.FromMinutes(4));
        options.BatchSize.Should().Be(50);
        options.PollingInterval.Should().Be(TimeSpan.FromSeconds(7));
    }

    [Fact]
    public void AddBudDataAccess_ShouldUseDefaultOutboxHealthCheckOptions_WhenSectionIsMissing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=bud;Username=postgres;Password=postgres"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddBudDataAccess(configuration);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<OutboxHealthCheckOptions>>().Value;
        options.MaxDeadLetters.Should().Be(0);
        options.MaxOldestPendingAge.Should().Be(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void AddBudDataAccess_ShouldUseDefaultOutboxProcessingOptions_WhenSectionIsMissing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=bud;Username=postgres;Password=postgres"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddBudDataAccess(configuration);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<OutboxProcessingOptions>>().Value;
        options.MaxRetries.Should().Be(5);
        options.BaseRetryDelay.Should().Be(TimeSpan.FromSeconds(5));
        options.MaxRetryDelay.Should().Be(TimeSpan.FromMinutes(5));
        options.BatchSize.Should().Be(100);
        options.PollingInterval.Should().Be(TimeSpan.FromSeconds(5));
    }
}
