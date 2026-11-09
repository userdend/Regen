using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Regen.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToScheduledBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "ScheduledBatches",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledBatches_UserId",
                table: "ScheduledBatches",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledBatches_Users_UserId",
                table: "ScheduledBatches",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledBatches_Users_UserId",
                table: "ScheduledBatches");

            migrationBuilder.DropIndex(
                name: "IX_ScheduledBatches_UserId",
                table: "ScheduledBatches");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ScheduledBatches");
        }
    }
}
