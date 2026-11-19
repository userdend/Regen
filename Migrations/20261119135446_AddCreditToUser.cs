using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Regen.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Credit",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 30);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Credit",
                table: "Users");
        }
    }
}
