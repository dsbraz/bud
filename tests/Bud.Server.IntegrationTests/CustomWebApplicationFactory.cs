using System.Net.Http.Headers;
using Bud.Server.Data;
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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add DbContext with Testcontainer connection string + tenant interceptor
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.UseNpgsql(_postgres.GetConnectionString());
                options.AddInterceptors(sp.GetRequiredService<TenantSaveChangesInterceptor>());
            });

            // Build service provider and apply migrations
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Ensure database is deleted and recreated with latest schema
            db.Database.EnsureDeleted();
            db.Database.Migrate();

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

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
}
