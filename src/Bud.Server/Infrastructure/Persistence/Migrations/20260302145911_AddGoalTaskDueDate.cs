using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bud.Server.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGoalTaskDueDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "due_date",
                table: "GoalTasks",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "due_date",
                table: "GoalTasks");
        }
    }
}
