using Bud.Server.MultiTenancy;
using Bud.Server.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Infrastructure.Persistence;

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
    public DbSet<Mission> Missions => Set<Mission>();
    public DbSet<Metric> MissionMetrics => Set<Metric>();
    public DbSet<CollaboratorTeam> CollaboratorTeams => Set<CollaboratorTeam>();
    public DbSet<MetricCheckin> MetricCheckins => Set<MetricCheckin>();
    public DbSet<MissionTemplate> MissionTemplates => Set<MissionTemplate>();
    public DbSet<MissionTemplateObjective> MissionTemplateObjectives => Set<MissionTemplateObjective>();
    public DbSet<MissionTemplateMetric> MissionTemplateMetrics => Set<MissionTemplateMetric>();
    public DbSet<Objective> MissionObjectives => Set<Objective>();
    public DbSet<CollaboratorAccessLog> CollaboratorAccessLogs => Set<CollaboratorAccessLog>();
    public DbSet<Notification> Notifications => Set<Notification>();

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

        modelBuilder.Entity<Mission>()
            .HasQueryFilter(m =>
                !_applyTenantFilter ||
                (_isGlobalAdmin && _tenantId == null) ||
                (_tenantId != null && m.OrganizationId == _tenantId)
            );

        modelBuilder.Entity<Objective>()
            .HasQueryFilter(mo =>
                !_applyTenantFilter ||
                (_isGlobalAdmin && _tenantId == null) ||
                (_tenantId != null && mo.OrganizationId == _tenantId)
            );

        modelBuilder.Entity<Metric>()
            .HasQueryFilter(mm =>
                !_applyTenantFilter ||
                (_isGlobalAdmin && _tenantId == null) ||
                (_tenantId != null && mm.OrganizationId == _tenantId)
            );

        modelBuilder.Entity<MetricCheckin>()
            .HasQueryFilter(mc =>
                !_applyTenantFilter ||
                (_isGlobalAdmin && _tenantId == null) ||
                (_tenantId != null && mc.OrganizationId == _tenantId)
            );

        modelBuilder.Entity<MissionTemplate>()
            .HasQueryFilter(mt =>
                !_applyTenantFilter ||
                (_isGlobalAdmin && _tenantId == null) ||
                (_tenantId != null && mt.OrganizationId == _tenantId)
            );

        modelBuilder.Entity<MissionTemplateObjective>()
            .HasQueryFilter(mto =>
                !_applyTenantFilter ||
                (_isGlobalAdmin && _tenantId == null) ||
                (_tenantId != null && mto.OrganizationId == _tenantId)
            );

        modelBuilder.Entity<MissionTemplateMetric>()
            .HasQueryFilter(mtm =>
                !_applyTenantFilter ||
                (_isGlobalAdmin && _tenantId == null) ||
                (_tenantId != null && mtm.OrganizationId == _tenantId)
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

    }
}
