using Bud.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Data;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Collaborator> Collaborators => Set<Collaborator>();
    public DbSet<Mission> Missions => Set<Mission>();
    public DbSet<MissionMetric> MissionMetrics => Set<MissionMetric>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organization>()
            .Property(o => o.Name)
            .HasMaxLength(200);

        modelBuilder.Entity<Organization>()
            .HasOne(o => o.Owner)
            .WithMany()
            .HasForeignKey(o => o.OwnerId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        modelBuilder.Entity<Workspace>()
            .Property(w => w.Name)
            .HasMaxLength(200);

        modelBuilder.Entity<Team>()
            .Property(t => t.Name)
            .HasMaxLength(200);

        modelBuilder.Entity<Collaborator>()
            .Property(c => c.FullName)
            .HasMaxLength(200);

        modelBuilder.Entity<Collaborator>()
            .Property(c => c.Email)
            .HasMaxLength(320);

        modelBuilder.Entity<Mission>()
            .Property(m => m.Name)
            .HasMaxLength(200);

        modelBuilder.Entity<MissionMetric>()
            .Property(m => m.Name)
            .HasMaxLength(200);

        modelBuilder.Entity<MissionMetric>()
            .Property(m => m.TargetText)
            .HasMaxLength(1000);

        modelBuilder.Entity<Organization>()
            .HasMany(o => o.Workspaces)
            .WithOne(w => w.Organization)
            .HasForeignKey(w => w.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Workspace>()
            .HasMany(w => w.Teams)
            .WithOne(t => t.Workspace)
            .HasForeignKey(t => t.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Team>()
            .HasMany(t => t.Collaborators)
            .WithOne(c => c.Team)
            .HasForeignKey(c => c.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Team>()
            .HasMany(t => t.SubTeams)
            .WithOne(t => t.ParentTeam)
            .HasForeignKey(t => t.ParentTeamId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Mission>()
            .HasMany(m => m.Metrics)
            .WithOne(metric => metric.Mission)
            .HasForeignKey(metric => metric.MissionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Mission>()
            .HasOne(m => m.Organization)
            .WithMany()
            .HasForeignKey(m => m.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

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
    }
}
