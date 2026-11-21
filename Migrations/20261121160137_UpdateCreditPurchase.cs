using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Regen.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCreditPurchase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreditPackageId",
                table: "CreditPurchases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StripePaymentIntentId",
                table: "CreditPurchases",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditPurchases_CreditPackageId",
                table: "CreditPurchases",
                column: "CreditPackageId");

            migrationBuilder.AddForeignKey(
                name: "FK_CreditPurchases_CreditPackages_CreditPackageId",
                table: "CreditPurchases",
                column: "CreditPackageId",
                principalTable: "CreditPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CreditPurchases_CreditPackages_CreditPackageId",
                table: "CreditPurchases");

            migrationBuilder.DropIndex(
                name: "IX_CreditPurchases_CreditPackageId",
                table: "CreditPurchases");

            migrationBuilder.DropColumn(
                name: "CreditPackageId",
                table: "CreditPurchases");

            migrationBuilder.DropColumn(
                name: "StripePaymentIntentId",
                table: "CreditPurchases");
        }
    }
}
