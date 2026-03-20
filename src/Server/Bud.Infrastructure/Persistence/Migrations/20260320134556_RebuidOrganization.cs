using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RebuidOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_Collaborators_OwnerId",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_OwnerId",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Organizations");

            migrationBuilder.AddColumn<string>(
                name: "Plan",
                table: "Organizations",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IconUrl",
                table: "Organizations",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Organizations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Organizations",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Plan", table: "Organizations");
            migrationBuilder.DropColumn(name: "IconUrl", table: "Organizations");
            migrationBuilder.DropColumn(name: "CreatedAt", table: "Organizations");
            migrationBuilder.DropColumn(name: "DeletedAt", table: "Organizations");

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "Organizations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_OwnerId",
                table: "Organizations",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_Collaborators_OwnerId",
                table: "Organizations",
                column: "OwnerId",
                principalTable: "Collaborators",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
