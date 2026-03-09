using Bud.Api.DependencyInjection;
using Bud.Api.MultiTenancy;
using Bud.Api.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBudPlatform(builder.Configuration, builder.Environment);

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
await app.SeedDatabaseAsync();

if (!app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders();
    app.UseHsts();
}

app.UseExceptionHandler();
app.UseMiddleware<LogEnrichmentMiddleware>();
app.UseMiddleware<Bud.Api.Middleware.SecurityHeadersMiddleware>();
app.UseHttpsRedirection();
app.UseMiddleware<Bud.Api.Middleware.RequestTelemetryMiddleware>();

app.UseRouting();
if (app.Environment.IsDevelopment())
{
    app.UseCors(BudApiCompositionExtensions.LocalDevelopmentCorsPolicy);
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
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

app.Run();

public partial class Program { }
