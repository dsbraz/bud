using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bud.Server.Migrations;
    /// <inheritdoc />
    public partial class UpdateMissionMetricFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TargetValue",
                table: "MissionMetrics",
                newName: "MinValue");

            migrationBuilder.AddColumn<decimal>(
                name: "MaxValue",
                table: "MissionMetrics",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuantitativeType",
                table: "MissionMetrics",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxValue",
                table: "MissionMetrics");

            migrationBuilder.DropColumn(
                name: "QuantitativeType",
                table: "MissionMetrics");

            migrationBuilder.RenameColumn(
                name: "MinValue",
                table: "MissionMetrics",
                newName: "TargetValue");
        }
    }
