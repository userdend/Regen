using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Regen.Migrations
{
    /// <inheritdoc />
    public partial class AddChargeCycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChargeCycle",
                table: "ScheduledEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChargeCycle",
                table: "ScheduledEntries");
        }
    }
}
