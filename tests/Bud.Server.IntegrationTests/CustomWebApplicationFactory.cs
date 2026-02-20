using System.Net.Http.Headers;
using Bud.Server.Infrastructure.Persistence;
using Bud.Server.IntegrationTests.Helpers;
using Bud.Server.MultiTenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Bud.Server.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("bud_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private string _connectionString = string.Empty;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // UseSetting applies configuration early enough for the minimal hosting model
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        builder.UseSetting("Jwt:Key", "CHANGE-THIS-KEY-IN-PRODUCTION-USE-AT-LEAST-32-CHARACTERS");
        builder.UseSetting("Jwt:Issuer", "bud-api");
        builder.UseSetting("Jwt:Audience", "bud-api");

        builder.ConfigureServices(services =>
        {
            // Remove existing ApplicationDbContext registration (registered as Scoped in Program.cs)
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ApplicationDbContext));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Re-register ApplicationDbContext with Testcontainer connection string
            services.AddScoped<ApplicationDbContext>(sp =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                optionsBuilder.UseNpgsql(_connectionString);
                optionsBuilder.AddInterceptors(sp.GetRequiredService<TenantSaveChangesInterceptor>());

                var tenantProvider = sp.GetRequiredService<ITenantProvider>();
                return new ApplicationDbContext(optionsBuilder.Options, tenantProvider);
            });

            // Build service provider and create database from model
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Recreate database from current model (no migrations needed)
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            // Seed bootstrap data
            DbSeeder.SeedAsync(db).Wait();
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// Creates an HttpClient with global admin JWT token.
    /// </summary>
    public HttpClient CreateGlobalAdminClient()
    {
        var client = CreateClient();
        var token = JwtTestHelper.GenerateGlobalAdminToken();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Creates an HttpClient with tenant user JWT token and optional X-Tenant-Id header.
    /// </summary>
    public HttpClient CreateTenantClient(Guid tenantId, string email, Guid collaboratorId)
    {
        var client = CreateClient();
        var token = JwtTestHelper.GenerateTenantUserToken(email, tenantId, collaboratorId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId.ToString());
        return client;
    }

    /// <summary>
    /// Creates an HttpClient with authenticated user JWT token but without tenant information.
    /// </summary>
    public HttpClient CreateUserClientWithoutTenant(string email)
    {
        var client = CreateClient();
        var token = JwtTestHelper.GenerateUserTokenWithoutTenant(email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _connectionString = _postgres.GetConnectionString();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
}
