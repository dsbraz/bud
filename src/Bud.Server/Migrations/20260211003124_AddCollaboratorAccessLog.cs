using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bud.Server.Migrations;

/// <inheritdoc />
public partial class AddCollaboratorAccessLog : Migration
{
    private static readonly string[] _indexColumns = ["OrganizationId", "AccessedAt"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CollaboratorAccessLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CollaboratorId = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                AccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CollaboratorAccessLogs", x => x.Id);
                table.ForeignKey(
                    name: "FK_CollaboratorAccessLogs_Collaborators_CollaboratorId",
                    column: x => x.CollaboratorId,
                    principalTable: "Collaborators",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CollaboratorAccessLogs_Organizations_OrganizationId",
                    column: x => x.OrganizationId,
                    principalTable: "Organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CollaboratorAccessLogs_CollaboratorId",
            table: "CollaboratorAccessLogs",
            column: "CollaboratorId");

        migrationBuilder.CreateIndex(
            name: "IX_CollaboratorAccessLogs_OrganizationId_AccessedAt",
            table: "CollaboratorAccessLogs",
            columns: _indexColumns);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CollaboratorAccessLogs");
    }
}
