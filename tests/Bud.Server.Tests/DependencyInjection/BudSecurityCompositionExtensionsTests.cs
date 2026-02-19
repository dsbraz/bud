using Bud.Server.DependencyInjection;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Bud.Server.Tests.DependencyInjection;

public sealed class BudSecurityCompositionExtensionsTests
{
    [Fact]
    public void AddBudAuthentication_InNonDevelopment_WithoutKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "bud-api",
                ["Jwt:Audience"] = "bud-api"
            })
            .Build();

        var environment = new Mock<IWebHostEnvironment>();
        environment.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        // Act
        var act = () => services.AddBudAuthentication(configuration, environment.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*JWT*obrigatória*");
    }

    [Fact]
    public void AddBudAuthentication_InDevelopment_WithoutKey_UsesDevFallback()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "bud-dev",
                ["Jwt:Audience"] = "bud-api"
            })
            .Build();

        var environment = new Mock<IWebHostEnvironment>();
        environment.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        // Act
        var act = () => services.AddBudAuthentication(configuration, environment.Object);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddBudAuthentication_InProduction_WithKey_Succeeds()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "production-secret-key-at-least-32-characters-long",
                ["Jwt:Issuer"] = "bud-api",
                ["Jwt:Audience"] = "bud-api"
            })
            .Build();

        var environment = new Mock<IWebHostEnvironment>();
        environment.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        // Act
        var act = () => services.AddBudAuthentication(configuration, environment.Object);

        // Assert
        act.Should().NotThrow();
    }
}
