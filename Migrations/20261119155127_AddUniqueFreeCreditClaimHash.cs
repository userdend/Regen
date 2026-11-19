using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Regen.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueFreeCreditClaimHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_FreeCreditClaims_GoogleIdHash",
                table: "FreeCreditClaims",
                column: "GoogleIdHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FreeCreditClaims_GoogleIdHash",
                table: "FreeCreditClaims");
        }
    }
}
