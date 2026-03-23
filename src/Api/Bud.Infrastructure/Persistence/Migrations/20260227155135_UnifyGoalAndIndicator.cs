using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UnifyGoalAndIndicator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MetricCheckins");

            migrationBuilder.DropTable(
                name: "MissionTemplateMetrics");

            migrationBuilder.DropTable(
                name: "MissionMetrics");

            migrationBuilder.DropTable(
                name: "MissionTemplateObjectives");

            migrationBuilder.DropTable(
                name: "MissionObjectives");

            migrationBuilder.DropTable(
                name: "MissionTemplates");

            migrationBuilder.DropTable(
                name: "Missions");

            migrationBuilder.DropTable(
                name: "ObjectiveDimensions");

            migrationBuilder.CreateTable(
                name: "Goals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Dimension = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    CollaboratorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Goals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Goals_Collaborators_CollaboratorId",
                        column: x => x.CollaboratorId,
                        principalTable: "Collaborators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Goals_Goals_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Goals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Goals_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Goals_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Goals_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    GoalNamePattern = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GoalDescriptionPattern = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Templates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Templates_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Indicators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    QuantitativeType = table.Column<int>(type: "integer", nullable: true),
                    MinValue = table.Column<decimal>(type: "numeric", nullable: true),
                    MaxValue = table.Column<decimal>(type: "numeric", nullable: true),
                    Unit = table.Column<int>(type: "integer", nullable: true),
                    TargetText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Indicators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Indicators_Goals_GoalId",
                        column: x => x.GoalId,
                        principalTable: "Goals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Indicators_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateGoals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    Dimension = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateGoals_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateGoals_TemplateGoals_ParentId",
                        column: x => x.ParentId,
                        principalTable: "TemplateGoals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateGoals_Templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "Templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Checkins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CollaboratorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<decimal>(type: "numeric", nullable: true),
                    Text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CheckinDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ConfidenceLevel = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Checkins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Checkins_Collaborators_CollaboratorId",
                        column: x => x.CollaboratorId,
                        principalTable: "Collaborators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Checkins_Indicators_IndicatorId",
                        column: x => x.IndicatorId,
                        principalTable: "Indicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Checkins_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateIndicators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateGoalId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_TemplateIndicators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateIndicators_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateIndicators_TemplateGoals_TemplateGoalId",
                        column: x => x.TemplateGoalId,
                        principalTable: "TemplateGoals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TemplateIndicators_Templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "Templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Checkins_CollaboratorId",
                table: "Checkins",
                column: "CollaboratorId");

            migrationBuilder.CreateIndex(
                name: "IX_Checkins_IndicatorId",
                table: "Checkins",
                column: "IndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Checkins_OrganizationId",
                table: "Checkins",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_CollaboratorId",
                table: "Goals",
                column: "CollaboratorId");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_OrganizationId",
                table: "Goals",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_ParentId",
                table: "Goals",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_TeamId",
                table: "Goals",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_WorkspaceId",
                table: "Goals",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Indicators_GoalId",
                table: "Indicators",
                column: "GoalId");

            migrationBuilder.CreateIndex(
                name: "IX_Indicators_OrganizationId",
                table: "Indicators",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateGoals_OrganizationId",
                table: "TemplateGoals",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateGoals_ParentId",
                table: "TemplateGoals",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateGoals_TemplateId",
                table: "TemplateGoals",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateIndicators_OrganizationId",
                table: "TemplateIndicators",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateIndicators_TemplateGoalId",
                table: "TemplateIndicators",
                column: "TemplateGoalId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateIndicators_TemplateId",
                table: "TemplateIndicators",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Templates_OrganizationId",
                table: "Templates",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Checkins");

            migrationBuilder.DropTable(
                name: "TemplateIndicators");

            migrationBuilder.DropTable(
                name: "Indicators");

            migrationBuilder.DropTable(
                name: "TemplateGoals");

            migrationBuilder.DropTable(
                name: "Goals");

            migrationBuilder.DropTable(
                name: "Templates");

            migrationBuilder.CreateTable(
                name: "Missions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CollaboratorId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Missions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Missions_Collaborators_CollaboratorId",
                        column: x => x.CollaboratorId,
                        principalTable: "Collaborators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Missions_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Missions_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Missions_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MissionTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MissionDescriptionPattern = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MissionNamePattern = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissionTemplates_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ObjectiveDimensions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectiveDimensions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObjectiveDimensions_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MissionObjectives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Dimension = table.Column<Guid>(type: "uuid", nullable: true),
                    MissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionObjectives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissionObjectives_Missions_MissionId",
                        column: x => x.MissionId,
                        principalTable: "Missions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MissionObjectives_ObjectiveDimensions_Dimension",
                        column: x => x.Dimension,
                        principalTable: "ObjectiveDimensions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MissionObjectives_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MissionTemplateObjectives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Dimension = table.Column<Guid>(type: "uuid", nullable: true),
                    MissionTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionTemplateObjectives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissionTemplateObjectives_MissionTemplates_MissionTemplateId",
                        column: x => x.MissionTemplateId,
                        principalTable: "MissionTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MissionTemplateObjectives_ObjectiveDimensions_Dimension",
                        column: x => x.Dimension,
                        principalTable: "ObjectiveDimensions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MissionTemplateObjectives_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MissionMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjectiveId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaxValue = table.Column<decimal>(type: "numeric", nullable: true),
                    MinValue = table.Column<decimal>(type: "numeric", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    QuantitativeType = table.Column<int>(type: "integer", nullable: true),
                    TargetText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Unit = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissionMetrics_MissionObjectives_ObjectiveId",
                        column: x => x.ObjectiveId,
                        principalTable: "MissionObjectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MissionMetrics_Missions_MissionId",
                        column: x => x.MissionId,
                        principalTable: "Missions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MissionMetrics_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MissionTemplateMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MissionTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    MissionTemplateObjectiveId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaxValue = table.Column<decimal>(type: "numeric", nullable: true),
                    MinValue = table.Column<decimal>(type: "numeric", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    QuantitativeType = table.Column<int>(type: "integer", nullable: true),
                    TargetText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Unit = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionTemplateMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissionTemplateMetrics_MissionTemplateObjectives_MissionTem~",
                        column: x => x.MissionTemplateObjectiveId,
                        principalTable: "MissionTemplateObjectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
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

            migrationBuilder.CreateTable(
                name: "MetricCheckins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CollaboratorId = table.Column<Guid>(type: "uuid", nullable: false),
                    MetricId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckinDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfidenceLevel = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Value = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricCheckins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MetricCheckins_Collaborators_CollaboratorId",
                        column: x => x.CollaboratorId,
                        principalTable: "Collaborators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MetricCheckins_MissionMetrics_MetricId",
                        column: x => x.MetricId,
                        principalTable: "MissionMetrics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MetricCheckins_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MetricCheckins_CollaboratorId",
                table: "MetricCheckins",
                column: "CollaboratorId");

            migrationBuilder.CreateIndex(
                name: "IX_MetricCheckins_MetricId",
                table: "MetricCheckins",
                column: "MetricId");

            migrationBuilder.CreateIndex(
                name: "IX_MetricCheckins_OrganizationId",
                table: "MetricCheckins",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionMetrics_MissionId",
                table: "MissionMetrics",
                column: "MissionId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionMetrics_ObjectiveId",
                table: "MissionMetrics",
                column: "ObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionMetrics_OrganizationId",
                table: "MissionMetrics",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionObjectives_Dimension",
                table: "MissionObjectives",
                column: "Dimension");

            migrationBuilder.CreateIndex(
                name: "IX_MissionObjectives_MissionId",
                table: "MissionObjectives",
                column: "MissionId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionObjectives_OrganizationId",
                table: "MissionObjectives",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Missions_CollaboratorId",
                table: "Missions",
                column: "CollaboratorId");

            migrationBuilder.CreateIndex(
                name: "IX_Missions_OrganizationId",
                table: "Missions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Missions_TeamId",
                table: "Missions",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Missions_WorkspaceId",
                table: "Missions",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionTemplateMetrics_MissionTemplateId",
                table: "MissionTemplateMetrics",
                column: "MissionTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionTemplateMetrics_MissionTemplateObjectiveId",
                table: "MissionTemplateMetrics",
                column: "MissionTemplateObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionTemplateMetrics_OrganizationId",
                table: "MissionTemplateMetrics",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionTemplateObjectives_Dimension",
                table: "MissionTemplateObjectives",
                column: "Dimension");

            migrationBuilder.CreateIndex(
                name: "IX_MissionTemplateObjectives_MissionTemplateId",
                table: "MissionTemplateObjectives",
                column: "MissionTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionTemplateObjectives_OrganizationId",
                table: "MissionTemplateObjectives",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionTemplates_OrganizationId",
                table: "MissionTemplates",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveDimensions_OrganizationId",
                table: "ObjectiveDimensions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveDimensions_OrganizationId_Name",
                table: "ObjectiveDimensions",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);
        }
    }
}
