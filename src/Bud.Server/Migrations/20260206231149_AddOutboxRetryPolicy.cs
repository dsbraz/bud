using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bud.Server.Migrations;

/// <inheritdoc />
public partial class AddOutboxRetryPolicy : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_OutboxMessages_ProcessedOnUtc_OccurredOnUtc",
            table: "OutboxMessages");

        migrationBuilder.AddColumn<DateTime>(
            name: "DeadLetteredOnUtc",
            table: "OutboxMessages",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "NextAttemptOnUtc",
            table: "OutboxMessages",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "RetryCount",
            table: "OutboxMessages",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        var processingStateColumns = new[] { "ProcessedOnUtc", "DeadLetteredOnUtc", "NextAttemptOnUtc", "OccurredOnUtc" };
        migrationBuilder.CreateIndex(
            name: "IX_OutboxMessages_ProcessedOnUtc_DeadLetteredOnUtc_NextAttempt~",
            table: "OutboxMessages",
            columns: processingStateColumns);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_OutboxMessages_ProcessedOnUtc_DeadLetteredOnUtc_NextAttempt~",
            table: "OutboxMessages");

        migrationBuilder.DropColumn(
            name: "DeadLetteredOnUtc",
            table: "OutboxMessages");

        migrationBuilder.DropColumn(
            name: "NextAttemptOnUtc",
            table: "OutboxMessages");

        migrationBuilder.DropColumn(
            name: "RetryCount",
            table: "OutboxMessages");

        var processedAndOccurredColumns = new[] { "ProcessedOnUtc", "OccurredOnUtc" };
        migrationBuilder.CreateIndex(
            name: "IX_OutboxMessages_ProcessedOnUtc_OccurredOnUtc",
            table: "OutboxMessages",
            columns: processedAndOccurredColumns);
    }
}
