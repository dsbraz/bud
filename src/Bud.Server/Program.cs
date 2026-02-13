using Bud.Server.DependencyInjection;
using Bud.Server.MultiTenancy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBudPlatform(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bud API v1");
    });
}

await app.EnsureDevelopmentDatabaseAsync();

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseMiddleware<Bud.Server.Middleware.RequestTelemetryMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantRequiredMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
