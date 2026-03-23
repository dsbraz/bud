using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Goals_Teams_TeamId",
                table: "Goals");

            migrationBuilder.DropForeignKey(
                name: "FK_Goals_Workspaces_WorkspaceId",
                table: "Goals");

            migrationBuilder.DropIndex(
                name: "IX_Goals_TeamId",
                table: "Goals");

            migrationBuilder.DropIndex(
                name: "IX_Goals_WorkspaceId",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "TeamId",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "Goals");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TeamId",
                table: "Goals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "Goals",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Goals_TeamId",
                table: "Goals",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_WorkspaceId",
                table: "Goals",
                column: "WorkspaceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Goals_Teams_TeamId",
                table: "Goals",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Goals_Workspaces_WorkspaceId",
                table: "Goals",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
