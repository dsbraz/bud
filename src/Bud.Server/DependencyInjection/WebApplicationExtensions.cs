using Bud.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.DependencyInjection;

public static partial class WebApplicationExtensions
{
    public static async Task EnsureDevelopmentDatabaseAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseSetup");

        const int maxAttempts = 5;
        var created = false;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                dbContext.Database.EnsureCreated();
                created = true;
                break;
            }
            catch (Exception ex)
            {
                LogDatabaseSetupAttemptFailed(logger, ex, attempt);
                Thread.Sleep(TimeSpan.FromSeconds(3));
            }
        }

        if (!created)
        {
            LogDatabaseSetupFailedAfterAttempts(logger, maxAttempts);
        }
    }

    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseSetup");

        await DbSeeder.SeedAsync(dbContext);
        LogDatabaseSeedCompleted(logger);
    }

    [LoggerMessage(
        EventId = 3500,
        Level = LogLevel.Warning,
        Message = "Database setup attempt {Attempt} failed.")]
    private static partial void LogDatabaseSetupAttemptFailed(ILogger logger, Exception exception, int attempt);

    [LoggerMessage(
        EventId = 3501,
        Level = LogLevel.Error,
        Message = "Database setup failed after {Attempts} attempts.")]
    private static partial void LogDatabaseSetupFailedAfterAttempts(ILogger logger, int attempts);

    [LoggerMessage(
        EventId = 3502,
        Level = LogLevel.Information,
        Message = "Database seed completed.")]
    private static partial void LogDatabaseSeedCompleted(ILogger logger);
}
