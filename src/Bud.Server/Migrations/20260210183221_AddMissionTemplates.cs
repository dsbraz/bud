using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bud.Server.Migrations;

/// <inheritdoc />
public partial class AddMissionTemplates : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MissionTemplates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                MissionNamePattern = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                MissionDescriptionPattern = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MissionTemplates", x => x.Id);
                table.ForeignKey(
                    name: "FK_MissionTemplates_Organizations_OrganizationId",
                    column: x => x.OrganizationId,
                    principalTable: "Organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MissionTemplateMetrics",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                MissionTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false),
                OrderIndex = table.Column<int>(type: "integer", nullable: false),
                QuantitativeType = table.Column<int>(type: "integer", nullable: true),
                MinValue = table.Column<decimal>(type: "numeric", nullable: true),
                MaxValue = table.Column<decimal>(type: "numeric", nullable: true),
                Unit = table.Column<int>(type: "integer", nullable: true),
                TargetText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MissionTemplateMetrics", x => x.Id);
                table.ForeignKey(
                    name: "FK_MissionTemplateMetrics_MissionTemplates_MissionTemplateId",
                    column: x => x.MissionTemplateId,
                    principalTable: "MissionTemplates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MissionTemplateMetrics_Organizations_OrganizationId",
                    column: x => x.OrganizationId,
                    principalTable: "Organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MissionTemplateMetrics_MissionTemplateId",
            table: "MissionTemplateMetrics",
            column: "MissionTemplateId");

        migrationBuilder.CreateIndex(
            name: "IX_MissionTemplateMetrics_OrganizationId",
            table: "MissionTemplateMetrics",
            column: "OrganizationId");

        migrationBuilder.CreateIndex(
            name: "IX_MissionTemplates_OrganizationId",
            table: "MissionTemplates",
            column: "OrganizationId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "MissionTemplateMetrics");

        migrationBuilder.DropTable(
            name: "MissionTemplates");
    }
}
