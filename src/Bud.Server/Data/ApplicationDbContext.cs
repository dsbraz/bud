using Bud.Server.MultiTenancy;
using Bud.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Data;

public sealed class ApplicationDbContext : DbContext
{
    private readonly Guid? _tenantId;
    private readonly bool _isAdmin;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantProvider? tenantProvider = null)
        : base(options)
    {
        _tenantId = tenantProvider?.TenantId;
        _isAdmin = tenantProvider?.IsAdmin ?? false;
    }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Collaborator> Collaborators => Set<Collaborator>();
    public DbSet<Mission> Missions => Set<Mission>();
    public DbSet<MissionMetric> MissionMetrics => Set<MissionMetric>();

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
            .OnDelete(DeleteBehavior.Cascade);

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

        // Global Query Filters for multi-tenancy
        modelBuilder.Entity<Organization>()
            .HasQueryFilter(o => _isAdmin || _tenantId == null || o.Id == _tenantId);

        modelBuilder.Entity<Workspace>()
            .HasQueryFilter(w => _isAdmin || _tenantId == null || w.OrganizationId == _tenantId);

        modelBuilder.Entity<Team>()
            .HasQueryFilter(t => _isAdmin || _tenantId == null || t.OrganizationId == _tenantId);

        modelBuilder.Entity<Collaborator>()
            .HasQueryFilter(c => _isAdmin || _tenantId == null || c.OrganizationId == _tenantId);

        modelBuilder.Entity<Mission>()
            .HasQueryFilter(m => _isAdmin || _tenantId == null || m.OrganizationId == _tenantId);

        modelBuilder.Entity<MissionMetric>()
            .HasQueryFilter(mm => _isAdmin || _tenantId == null || mm.OrganizationId == _tenantId);
    }
}
