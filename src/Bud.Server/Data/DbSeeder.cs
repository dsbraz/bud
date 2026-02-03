using Bud.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Check if seed already exists
        // Must ignore query filters since we're seeding without tenant context
        if (await context.Organizations.IgnoreQueryFilters().AnyAsync(o => o.Name == "getbud.co"))
        {
            return;
        }

        // 1. Create Organization "Bud" (without owner initially)
        var budOrg = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "getbud.co",
            OwnerId = null
        };
        context.Organizations.Add(budOrg);

        // 2. Create Workspace "Bud"
        var budWorkspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Bud",
            Visibility = Visibility.Public,
            OrganizationId = budOrg.Id
        };
        context.Workspaces.Add(budWorkspace);

        // 3. Create Team "Bud"
        var budTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Bud",
            OrganizationId = budOrg.Id,
            WorkspaceId = budWorkspace.Id
        };
        context.Teams.Add(budTeam);

        // 4. Create Global Admin Leader
        var adminLeader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Administrador Global",
            Email = "admin@getbud.co",
            Role = CollaboratorRole.Leader,
            OrganizationId = budOrg.Id,
            TeamId = budTeam.Id
        };
        context.Collaborators.Add(adminLeader);

        await context.SaveChangesAsync();

        // 5. Update Organization with Owner
        budOrg.OwnerId = adminLeader.Id;
        await context.SaveChangesAsync();
    }
}
