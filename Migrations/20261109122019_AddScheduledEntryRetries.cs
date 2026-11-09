using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Regen.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledEntryRetries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastHttpStatusCode",
                table: "ScheduledEntries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRetryAt",
                table: "ScheduledEntries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "ScheduledEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastHttpStatusCode",
                table: "ScheduledEntries");

            migrationBuilder.DropColumn(
                name: "NextRetryAt",
                table: "ScheduledEntries");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "ScheduledEntries");
        }
    }
}
