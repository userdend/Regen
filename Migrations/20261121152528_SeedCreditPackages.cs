using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Regen.Migrations
{
    /// <inheritdoc />
    public partial class SeedCreditPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "CreditPackages",
                columns: new[] { "Id", "Amount", "CreatedAt", "Credits", "Currency", "IsActive", "Name", "StripePriceId", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 3.00m, new DateTime(2026, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc), 30, "MYR", true, "Starter", "price_1UeFhQHaga1NVVh0rM4EpQGi", new DateTime(2026, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, 10.00m, new DateTime(2026, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc), 100, "MYR", true, "Basic", "price_1UeFizHaga1NVVh0OhHp23fW", new DateTime(2026, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, 45.00m, new DateTime(2026, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc), 500, "MYR", true, "Pro", "price_1UeFjZHaga1NVVh0uiihyUB4", new DateTime(2026, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, 80.00m, new DateTime(2026, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc), 1000, "MYR", true, "Premium", "price_1UeFk3Haga1NVVh0T0bCVxQC", new DateTime(2026, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "CreditPackages",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "CreditPackages",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "CreditPackages",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "CreditPackages",
                keyColumn: "Id",
                keyValue: 4);
        }
    }
}
