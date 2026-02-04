using Bud.Server.Data;
using Bud.Server.Middleware;
using Bud.Server.MultiTenancy;
using Bud.Server.Services;
using Bud.Server.Settings;
using Bud.Server.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add controllers and API explorer
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add ProblemDetails and exception handling
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Add Settings
builder.Services.Configure<GlobalAdminSettings>(builder.Configuration.GetSection("GlobalAdminSettings"));

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrganizationValidator>();

// Add JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-secret-key-change-in-production-minimum-32-characters-required";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "bud-dev";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "bud-api";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Add Multi-Tenancy
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, JwtTenantProvider>();
builder.Services.AddScoped<TenantSaveChangesInterceptor>();

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not configured.");
}

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    options.UseNpgsql(connectionString);
    options.AddInterceptors(sp.GetRequiredService<TenantSaveChangesInterceptor>());
});

// Add Services
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();
builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<ICollaboratorService, CollaboratorService>();
builder.Services.AddScoped<IMissionService, MissionService>();
builder.Services.AddScoped<IMissionMetricService, MissionMetricService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITenantAuthorizationService, TenantAuthorizationService>();
builder.Services.AddScoped<IOrganizationAuthorizationService, OrganizationAuthorizationService>();

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres", tags: ["db", "ready"]);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Apply migrations in development
if (app.Environment.IsDevelopment())
{
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
    }
    else
    {
        // Run seed after successful migration
        await DbSeeder.SeedAsync(dbContext);
        logger.LogInformation("Database seed completed.");
    }
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// Authentication & Authorization - MUST be before TenantRequiredMiddleware
app.UseAuthentication();
app.UseAuthorization();

// Multi-tenancy middleware — must be before MapControllers
app.UseMiddleware<TenantRequiredMiddleware>();

app.MapControllers();

// Health check endpoints
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // No checks for liveness
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapFallbackToFile("index.html");

app.Run();

// Make the implicit Program class public for testing
public partial class Program { }
