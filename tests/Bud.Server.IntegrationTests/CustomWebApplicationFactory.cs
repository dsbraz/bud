using Bud.Server.Data;
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
    /// Creates an HttpClient with admin headers (bypasses TenantRequiredMiddleware).
    /// </summary>
    public HttpClient CreateAdminClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Email", "admin@getbud.co");
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
