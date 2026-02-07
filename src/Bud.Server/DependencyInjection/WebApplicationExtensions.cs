using Bud.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.DependencyInjection;

public static class WebApplicationExtensions
{
    public static async Task ApplyDevelopmentMigrationsAndSeedAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseMigration");

        const int maxAttempts = 5;
        var migrated = false;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                dbContext.Database.Migrate();
                migrated = true;
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Migration attempt {Attempt} failed.", attempt);
                Thread.Sleep(TimeSpan.FromSeconds(3));
            }
        }

        if (!migrated)
        {
            logger.LogError("Database migration failed after {Attempts} attempts.", maxAttempts);
            return;
        }

        await DbSeeder.SeedAsync(dbContext);
        logger.LogInformation("Database seed completed.");
    }
}
