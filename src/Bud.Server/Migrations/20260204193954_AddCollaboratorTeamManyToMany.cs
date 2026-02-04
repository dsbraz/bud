using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bud.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddCollaboratorTeamManyToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CollaboratorTeams",
                columns: table => new
                {
                    CollaboratorId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollaboratorTeams", x => new { x.CollaboratorId, x.TeamId });
                    table.ForeignKey(
                        name: "FK_CollaboratorTeams_Collaborators_CollaboratorId",
                        column: x => x.CollaboratorId,
                        principalTable: "Collaborators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CollaboratorTeams_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollaboratorTeams_CollaboratorId",
                table: "CollaboratorTeams",
                column: "CollaboratorId");

            migrationBuilder.CreateIndex(
                name: "IX_CollaboratorTeams_TeamId",
                table: "CollaboratorTeams",
                column: "TeamId");

            // Migrate existing TeamId data to junction table
            migrationBuilder.Sql(@"
                INSERT INTO ""CollaboratorTeams"" (""CollaboratorId"", ""TeamId"", ""AssignedAt"")
                SELECT ""Id"", ""TeamId"", NOW()
                FROM ""Collaborators""
                WHERE ""TeamId"" IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollaboratorTeams");
        }
    }
}
