using Bud.Application.Ports;
using Bud.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Bud.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext
{
    private readonly Guid? _tenantId;
    private readonly bool _isGlobalAdmin;
    private readonly string? _userEmail;
    private readonly bool _applyTenantFilter;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantProvider? tenantProvider = null)
        : base(options)
    {
        _applyTenantFilter = tenantProvider is not null;
        _tenantId = tenantProvider?.TenantId;
        _isGlobalAdmin = tenantProvider?.IsGlobalAdmin ?? false;
        _userEmail = tenantProvider?.UserEmail;
    }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Collaborator> Collaborators => Set<Collaborator>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<Indicator> Indicators => Set<Indicator>();
    public DbSet<CollaboratorTeam> CollaboratorTeams => Set<CollaboratorTeam>();
    public DbSet<Checkin> Checkins => Set<Checkin>();
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<TemplateGoal> TemplateGoals => Set<TemplateGoal>();
    public DbSet<TemplateIndicator> TemplateIndicators => Set<TemplateIndicator>();
    public DbSet<CollaboratorAccessLog> CollaboratorAccessLogs => Set<CollaboratorAccessLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<GoalTask> GoalTasks => Set<GoalTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Global Query Filters for multi-tenancy (simplified for performance)
        // SECURITY: Tenant ownership validation is now done in TenantRequiredMiddleware
        // These filters provide basic data isolation only
        // Global admins see all ONLY when no tenant is selected; otherwise they see the selected tenant
        modelBuilder.Entity<Organization>()
            .HasQueryFilter(o =>
                !_applyTenantFilter || // No tenant provider (schema creation/tests)
                (_isGlobalAdmin && _tenantId == null) || // Global admin with no tenant selected sees all
                (_tenantId != null && o.Id == _tenantId) // Anyone with tenant selected sees only that tenant
            );

        modelBuilder.Entity<Workspace>()
            .HasQueryFilter(w =>
                !_applyTenantFilter ||
                (_isGlobalAdmin && _tenantId == null) ||
                (_tenantId != null && w.OrganizationId == _tenantId)
            );

        modelBuilder.Entity<Team>()
            .HasQueryFilter(t =>
                !_applyTenantFilter ||
                (_isGlobalAdmin && _tenantId == null) ||
                (_tenantId != null && t.OrganizationId == _tenantId)
            );

        modelBuilder.Entity<Collaborator>()
            .HasQueryFilter(c =>
                !_applyTenantFilter ||
                (_isGlobalAdmin && _tenantId == null) ||
                (_tenantId != null && c.OrganizationId == _tenantId)
            );

        modelBuilder.Entity<Goal>()
            .HasQueryFilter(g =>
                !_applyTenantFilter ||
                (_isGlobalAdmin && _tenantId == null) ||
                (_tenantId != null && g.OrganizationId == _tenantId)
            );

        modelBuilder.Entity<Indicator>()
            .HasQueryFilter(i =>
                !_applyTenantFilter ||
                (_isGlobalAdmin && _tenantId == null) ||
                (_tenantId != null && i.OrganizationId == _tenantId)
            );

        modelBuilder.Entity<Checkin>()
            .HasQueryFilter(c =>
                !_applyTenantFilter ||
                (_isGlobalAdmin && _tenantId == null) ||
                (_tenantId != null && c.OrganizationId == _tenantId)
            );

        modelBuilder.Entity<Template>()
            .HasQueryFilter(mt =>
                !_applyTenantFilter ||
                (_isGlobalAdmin && _tenantId == null) ||
                (_tenantId != null && mt.OrganizationId == _tenantId)
            );

        modelBuilder.Entity<TemplateGoal>()
            .HasQueryFilter(tg =>
                !_applyTenantFilter ||
                (_isGlobalAdmin && _tenantId == null) ||
                (_tenantId != null && tg.OrganizationId == _tenantId)
            );

        modelBuilder.Entity<TemplateIndicator>()
            .HasQueryFilter(ti =>
                !_applyTenantFilter ||
                (_isGlobalAdmin && _tenantId == null) ||
                (_tenantId != null && ti.OrganizationId == _tenantId)
            );

        modelBuilder.Entity<CollaboratorAccessLog>()
            .HasQueryFilter(cal =>
                !_applyTenantFilter ||
                (_isGlobalAdmin && _tenantId == null) ||
                (_tenantId != null && cal.OrganizationId == _tenantId)
            );

        modelBuilder.Entity<Notification>()
            .HasQueryFilter(n =>
                !_applyTenantFilter ||
                (_isGlobalAdmin && _tenantId == null) ||
                (_tenantId != null && n.OrganizationId == _tenantId)
            );

        modelBuilder.Entity<GoalTask>()
            .HasQueryFilter(gt =>
                !_applyTenantFilter ||
                (_isGlobalAdmin && _tenantId == null) ||
                (_tenantId != null && gt.OrganizationId == _tenantId)
            );
    }
}
