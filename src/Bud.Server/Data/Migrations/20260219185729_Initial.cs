using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bud.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
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
                });

            migrationBuilder.CreateTable(
                name: "Collaborators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    LeaderId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collaborators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Collaborators_Collaborators_LeaderId",
                        column: x => x.LeaderId,
                        principalTable: "Collaborators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Organizations_Collaborators_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Collaborators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MissionTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MissionNamePattern = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MissionDescriptionPattern = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
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
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientCollaboratorId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Collaborators_RecipientCollaboratorId",
                        column: x => x.RecipientCollaboratorId,
                        principalTable: "Collaborators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifications_Organizations_OrganizationId",
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
                name: "Workspaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workspaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workspaces_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MissionTemplateObjectives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MissionTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    ObjectiveDimensionId = table.Column<Guid>(type: "uuid", nullable: true)
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
                        name: "FK_MissionTemplateObjectives_ObjectiveDimensions_ObjectiveDime~",
                        column: x => x.ObjectiveDimensionId,
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
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentTeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    LeaderId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teams_Collaborators_LeaderId",
                        column: x => x.LeaderId,
                        principalTable: "Collaborators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Teams_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Teams_Teams_ParentTeamId",
                        column: x => x.ParentTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Teams_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
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
                    MissionTemplateObjectiveId = table.Column<Guid>(type: "uuid", nullable: true),
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
                name: "CollaboratorTeams",
                columns: table => new
                {
                    CollaboratorId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollaboratorTeams", x => new { x.CollaboratorId, x.TeamId });
                    table.ForeignKey(
                        name: "FK_CollaboratorTeams_Collaborators_CollaboratorId",
                        column: x => x.CollaboratorId,
                        principalTable: "Collaborators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CollaboratorTeams_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Missions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    CollaboratorId = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "MissionObjectives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ObjectiveDimensionId = table.Column<Guid>(type: "uuid", nullable: true)
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
                        name: "FK_MissionObjectives_ObjectiveDimensions_ObjectiveDimensionId",
                        column: x => x.ObjectiveDimensionId,
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
                name: "MissionMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MissionObjectiveId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_MissionMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissionMetrics_MissionObjectives_MissionObjectiveId",
                        column: x => x.MissionObjectiveId,
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
                name: "MetricCheckins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MissionMetricId = table.Column<Guid>(type: "uuid", nullable: false),
                    CollaboratorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<decimal>(type: "numeric", nullable: true),
                    Text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CheckinDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ConfidenceLevel = table.Column<int>(type: "integer", nullable: false)
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
                        name: "FK_MetricCheckins_MissionMetrics_MissionMetricId",
                        column: x => x.MissionMetricId,
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
                name: "IX_CollaboratorAccessLogs_CollaboratorId",
                table: "CollaboratorAccessLogs",
                column: "CollaboratorId");

            migrationBuilder.CreateIndex(
                name: "IX_CollaboratorAccessLogs_OrganizationId_AccessedAt",
                table: "CollaboratorAccessLogs",
                columns: new[] { "OrganizationId", "AccessedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Collaborators_Email",
                table: "Collaborators",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Collaborators_LeaderId",
                table: "Collaborators",
                column: "LeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_Collaborators_OrganizationId",
                table: "Collaborators",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Collaborators_TeamId",
                table: "Collaborators",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_CollaboratorTeams_CollaboratorId",
                table: "CollaboratorTeams",
                column: "CollaboratorId");

            migrationBuilder.CreateIndex(
                name: "IX_CollaboratorTeams_TeamId",
                table: "CollaboratorTeams",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_MetricCheckins_CollaboratorId",
                table: "MetricCheckins",
                column: "CollaboratorId");

            migrationBuilder.CreateIndex(
                name: "IX_MetricCheckins_MissionMetricId",
                table: "MetricCheckins",
                column: "MissionMetricId");

            migrationBuilder.CreateIndex(
                name: "IX_MetricCheckins_OrganizationId",
                table: "MetricCheckins",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionMetrics_MissionId",
                table: "MissionMetrics",
                column: "MissionId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionMetrics_MissionObjectiveId",
                table: "MissionMetrics",
                column: "MissionObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionMetrics_OrganizationId",
                table: "MissionMetrics",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionObjectives_MissionId",
                table: "MissionObjectives",
                column: "MissionId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionObjectives_ObjectiveDimensionId",
                table: "MissionObjectives",
                column: "ObjectiveDimensionId");

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
                name: "IX_MissionTemplateObjectives_MissionTemplateId",
                table: "MissionTemplateObjectives",
                column: "MissionTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionTemplateObjectives_ObjectiveDimensionId",
                table: "MissionTemplateObjectives",
                column: "ObjectiveDimensionId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionTemplateObjectives_OrganizationId",
                table: "MissionTemplateObjectives",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionTemplates_OrganizationId",
                table: "MissionTemplates",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_OrganizationId",
                table: "Notifications",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientCollaboratorId_IsRead_CreatedAtUtc",
                table: "Notifications",
                columns: new[] { "RecipientCollaboratorId", "IsRead", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveDimensions_OrganizationId",
                table: "ObjectiveDimensions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveDimensions_OrganizationId_Name",
                table: "ObjectiveDimensions",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_OwnerId",
                table: "Organizations",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_LeaderId",
                table: "Teams",
                column: "LeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_OrganizationId",
                table: "Teams",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_ParentTeamId",
                table: "Teams",
                column: "ParentTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_WorkspaceId",
                table: "Teams",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Workspaces_OrganizationId",
                table: "Workspaces",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_CollaboratorAccessLogs_Collaborators_CollaboratorId",
                table: "CollaboratorAccessLogs",
                column: "CollaboratorId",
                principalTable: "Collaborators",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CollaboratorAccessLogs_Organizations_OrganizationId",
                table: "CollaboratorAccessLogs",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Collaborators_Organizations_OrganizationId",
                table: "Collaborators",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Collaborators_Teams_TeamId",
                table: "Collaborators",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_Collaborators_OwnerId",
                table: "Organizations");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Collaborators_LeaderId",
                table: "Teams");

            migrationBuilder.DropTable(
                name: "CollaboratorAccessLogs");

            migrationBuilder.DropTable(
                name: "CollaboratorTeams");

            migrationBuilder.DropTable(
                name: "MetricCheckins");

            migrationBuilder.DropTable(
                name: "MissionTemplateMetrics");

            migrationBuilder.DropTable(
                name: "Notifications");

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

            migrationBuilder.DropTable(
                name: "Collaborators");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "Workspaces");

            migrationBuilder.DropTable(
                name: "Organizations");
        }
    }
}
