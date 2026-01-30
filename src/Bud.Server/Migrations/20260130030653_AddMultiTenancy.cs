using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bud.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Teams",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "OrganizationId",
                table: "Missions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "MissionMetrics",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Collaborators",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Teams_OrganizationId",
                table: "Teams",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionMetrics_OrganizationId",
                table: "MissionMetrics",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Collaborators_OrganizationId",
                table: "Collaborators",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Collaborators_Organizations_OrganizationId",
                table: "Collaborators",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MissionMetrics_Organizations_OrganizationId",
                table: "MissionMetrics",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Organizations_OrganizationId",
                table: "Teams",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Collaborators_Organizations_OrganizationId",
                table: "Collaborators");

            migrationBuilder.DropForeignKey(
                name: "FK_MissionMetrics_Organizations_OrganizationId",
                table: "MissionMetrics");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Organizations_OrganizationId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_OrganizationId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_MissionMetrics_OrganizationId",
                table: "MissionMetrics");

            migrationBuilder.DropIndex(
                name: "IX_Collaborators_OrganizationId",
                table: "Collaborators");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "MissionMetrics");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Collaborators");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrganizationId",
                table: "Missions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }
    }
}
