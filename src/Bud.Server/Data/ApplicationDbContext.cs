using Bud.Server.MultiTenancy;
using Bud.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Data;

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
    public DbSet<MissionMetric> MissionMetrics => Set<MissionMetric>();
    public DbSet<CollaboratorTeam> CollaboratorTeams => Set<CollaboratorTeam>();
    public DbSet<MetricCheckin> MetricCheckins => Set<MetricCheckin>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Organization
        modelBuilder.Entity<Organization>()
            .Property(o => o.Name)
            .HasMaxLength(200);

        modelBuilder.Entity<Organization>()
            .HasOne(o => o.Owner)
            .WithMany()
            .HasForeignKey(o => o.OwnerId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // Workspace
        modelBuilder.Entity<Workspace>()
            .Property(w => w.Name)
            .HasMaxLength(200);

        modelBuilder.Entity<Organization>()
            .HasMany(o => o.Workspaces)
            .WithOne(w => w.Organization)
            .HasForeignKey(w => w.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Team
        modelBuilder.Entity<Team>()
            .Property(t => t.Name)
            .HasMaxLength(200);

        modelBuilder.Entity<Team>()
            .HasOne(t => t.Organization)
            .WithMany()
            .HasForeignKey(t => t.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Team>()
            .HasIndex(t => t.OrganizationId);

        modelBuilder.Entity<Workspace>()
            .HasMany(w => w.Teams)
            .WithOne(t => t.Workspace)
            .HasForeignKey(t => t.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Team>()
            .HasMany(t => t.SubTeams)
            .WithOne(t => t.ParentTeam)
            .HasForeignKey(t => t.ParentTeamId)
            .OnDelete(DeleteBehavior.Restrict);

        // Collaborator
        modelBuilder.Entity<Collaborator>()
            .Property(c => c.FullName)
            .HasMaxLength(200);

        modelBuilder.Entity<Collaborator>()
            .Property(c => c.Email)
            .HasMaxLength(320);

        modelBuilder.Entity<Collaborator>()
            .HasOne(c => c.Organization)
            .WithMany()
            .HasForeignKey(c => c.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Collaborator>()
            .HasIndex(c => c.OrganizationId);

        modelBuilder.Entity<Team>()
            .HasMany(t => t.Collaborators)
            .WithOne(c => c.Team)
            .HasForeignKey(c => c.TeamId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        modelBuilder.Entity<Collaborator>()
            .HasOne(c => c.Leader)
            .WithMany()
            .HasForeignKey(c => c.LeaderId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        modelBuilder.Entity<Collaborator>()
            .HasIndex(c => c.Email)
            .IsUnique();

        // CollaboratorTeam (many-to-many junction table)
        modelBuilder.Entity<CollaboratorTeam>()
            .HasKey(ct => new { ct.CollaboratorId, ct.TeamId });

        modelBuilder.Entity<CollaboratorTeam>()
            .HasOne(ct => ct.Collaborator)
            .WithMany(c => c.CollaboratorTeams)
            .HasForeignKey(ct => ct.CollaboratorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CollaboratorTeam>()
            .HasOne(ct => ct.Team)
            .WithMany(t => t.CollaboratorTeams)
            .HasForeignKey(ct => ct.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CollaboratorTeam>()
            .HasIndex(ct => ct.CollaboratorId);

        modelBuilder.Entity<CollaboratorTeam>()
            .HasIndex(ct => ct.TeamId);

        // Mission
        modelBuilder.Entity<Mission>()
            .Property(m => m.Name)
            .HasMaxLength(200);

        modelBuilder.Entity<Mission>()
            .HasOne(m => m.Organization)
            .WithMany()
            .HasForeignKey(m => m.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        modelBuilder.Entity<Mission>()
            .HasIndex(m => m.OrganizationId);

        modelBuilder.Entity<Mission>()
            .HasOne(m => m.Workspace)
            .WithMany()
            .HasForeignKey(m => m.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Mission>()
            .HasOne(m => m.Team)
            .WithMany()
            .HasForeignKey(m => m.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Mission>()
            .HasOne(m => m.Collaborator)
            .WithMany()
            .HasForeignKey(m => m.CollaboratorId)
            .OnDelete(DeleteBehavior.Cascade);

        // MissionMetric
        modelBuilder.Entity<MissionMetric>()
            .Property(m => m.Name)
            .HasMaxLength(200);

        modelBuilder.Entity<MissionMetric>()
            .Property(m => m.TargetText)
            .HasMaxLength(1000);

        modelBuilder.Entity<MissionMetric>()
            .HasOne(mm => mm.Organization)
            .WithMany()
            .HasForeignKey(mm => mm.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MissionMetric>()
            .HasIndex(mm => mm.OrganizationId);

        modelBuilder.Entity<Mission>()
            .HasMany(m => m.Metrics)
            .WithOne(metric => metric.Mission)
            .HasForeignKey(metric => metric.MissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Global Query Filters for multi-tenancy (simplified for performance)
        // SECURITY: Tenant ownership validation is now done in TenantRequiredMiddleware
        // These filters provide basic data isolation only
        // Global admins see all ONLY when no tenant is selected; otherwise they see the selected tenant
        modelBuilder.Entity<Organization>()
            .HasQueryFilter(o =>
                !_applyTenantFilter || // No tenant provider (migrations/tests)
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

        modelBuilder.Entity<MissionMetric>()
            .HasQueryFilter(mm =>
                !_applyTenantFilter ||
                (_isGlobalAdmin && _tenantId == null) ||
                (_tenantId != null && mm.OrganizationId == _tenantId)
            );

        // MetricCheckin
        modelBuilder.Entity<MetricCheckin>()
            .Property(mc => mc.Note)
            .HasMaxLength(1000);

        modelBuilder.Entity<MetricCheckin>()
            .Property(mc => mc.Text)
            .HasMaxLength(1000);

        modelBuilder.Entity<MetricCheckin>()
            .HasOne(mc => mc.Organization)
            .WithMany()
            .HasForeignKey(mc => mc.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MetricCheckin>()
            .HasIndex(mc => mc.OrganizationId);

        modelBuilder.Entity<MetricCheckin>()
            .HasOne(mc => mc.Collaborator)
            .WithMany()
            .HasForeignKey(mc => mc.CollaboratorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MissionMetric>()
            .HasMany(mm => mm.Checkins)
            .WithOne(mc => mc.MissionMetric)
            .HasForeignKey(mc => mc.MissionMetricId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MetricCheckin>()
            .HasQueryFilter(mc =>
                !_applyTenantFilter ||
                (_isGlobalAdmin && _tenantId == null) ||
                (_tenantId != null && mc.OrganizationId == _tenantId)
            );

        // Outbox
        modelBuilder.Entity<OutboxMessage>()
            .HasKey(o => o.Id);

        modelBuilder.Entity<OutboxMessage>()
            .Property(o => o.EventType)
            .HasMaxLength(1000);

        modelBuilder.Entity<OutboxMessage>()
            .Property(o => o.Payload)
            .HasColumnType("text");

        modelBuilder.Entity<OutboxMessage>()
            .Property(o => o.Error)
            .HasColumnType("text");

        modelBuilder.Entity<OutboxMessage>()
            .HasIndex(o => new { o.ProcessedOnUtc, o.DeadLetteredOnUtc, o.NextAttemptOnUtc, o.OccurredOnUtc });
    }
}
