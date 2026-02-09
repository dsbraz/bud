using Bud.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.DependencyInjection;

public static partial class WebApplicationExtensions
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
                LogMigrationAttemptFailed(logger, ex, attempt);
                Thread.Sleep(TimeSpan.FromSeconds(3));
            }
        }

        if (!migrated)
        {
            LogMigrationFailedAfterAttempts(logger, maxAttempts);
            return;
        }

        await DbSeeder.SeedAsync(dbContext);
        LogDatabaseSeedCompleted(logger);
    }

    [LoggerMessage(
        EventId = 3500,
        Level = LogLevel.Warning,
        Message = "Migration attempt {Attempt} failed.")]
    private static partial void LogMigrationAttemptFailed(ILogger logger, Exception exception, int attempt);

    [LoggerMessage(
        EventId = 3501,
        Level = LogLevel.Error,
        Message = "Database migration failed after {Attempts} attempts.")]
    private static partial void LogMigrationFailedAfterAttempts(ILogger logger, int attempts);

    [LoggerMessage(
        EventId = 3502,
        Level = LogLevel.Information,
        Message = "Database seed completed.")]
    private static partial void LogDatabaseSeedCompleted(ILogger logger);
}
