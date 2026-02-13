using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bud.Server.Migrations;

/// <inheritdoc />
public partial class RemoveOutboxMessages : Migration
{
    private static readonly string[] DownIndexColumns = ["ProcessedOnUtc", "DeadLetteredOnUtc", "NextAttemptOnUtc", "OccurredOnUtc"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "OutboxMessages");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "OutboxMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DeadLetteredOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Error = table.Column<string>(type: "text", nullable: true),
                EventType = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                NextAttemptOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                OccurredOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Payload = table.Column<string>(type: "text", nullable: false),
                ProcessedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                RetryCount = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OutboxMessages", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_OutboxMessages_ProcessedOnUtc_DeadLetteredOnUtc_NextAttempt~",
            table: "OutboxMessages",
            columns: DownIndexColumns);
    }
}
