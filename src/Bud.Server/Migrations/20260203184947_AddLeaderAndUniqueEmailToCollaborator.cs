using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bud.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaderAndUniqueEmailToCollaborator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LeaderId",
                table: "Collaborators",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Collaborators_Email",
                table: "Collaborators",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Collaborators_LeaderId",
                table: "Collaborators",
                column: "LeaderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Collaborators_Collaborators_LeaderId",
                table: "Collaborators",
                column: "LeaderId",
                principalTable: "Collaborators",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Collaborators_Collaborators_LeaderId",
                table: "Collaborators");

            migrationBuilder.DropIndex(
                name: "IX_Collaborators_Email",
                table: "Collaborators");

            migrationBuilder.DropIndex(
                name: "IX_Collaborators_LeaderId",
                table: "Collaborators");

            migrationBuilder.DropColumn(
                name: "LeaderId",
                table: "Collaborators");
        }
    }
}
