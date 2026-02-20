using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bud.Server.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIsGlobalAdminToCollaborator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsGlobalAdmin",
                table: "Collaborators",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsGlobalAdmin",
                table: "Collaborators");
        }
    }
}
