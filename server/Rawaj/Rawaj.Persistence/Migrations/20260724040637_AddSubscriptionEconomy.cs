using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Rawaj.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionEconomy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExtraBrandsPurchased",
                table: "tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExtraMarketeersPurchased",
                table: "tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FreeContentGenerationsRemaining",
                table: "tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FreeImageGenerationsRemaining",
                table: "tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "FreeMarketingPlanUsed",
                table: "tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CoinUsageDiscountPercent",
                table: "subscription_plans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "billing_transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AmountUsd = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CoinsGranted = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_billing_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_billing_transactions_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "coin_packages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Coins = table.Column<int>(type: "int", nullable: false),
                    BonusCoins = table.Column<int>(type: "int", nullable: false),
                    PriceUsd = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coin_packages", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "coin_packages",
                columns: new[] { "Id", "BonusCoins", "Coins", "CreatedAt", "IsActive", "Name", "PriceUsd" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000101"), 0, 5000, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Starter", 50m },
                    { new Guid("00000000-0000-0000-0000-000000000102"), 1000, 10000, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Growth", 100m },
                    { new Guid("00000000-0000-0000-0000-000000000103"), 3000, 20000, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Business", 200m },
                    { new Guid("00000000-0000-0000-0000-000000000104"), 8000, 40000, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Enterprise", 400m }
                });

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "CoinUsageDiscountPercent", "MaxUsers" },
                values: new object[] { 0, 1 });

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                columns: new[] { "CoinUsageDiscountPercent", "Cost", "MaxAiCreditsMonthly", "MaxBrands", "MaxCampaignsMonthly", "MaxScheduledPosts", "MaxSocialAccounts", "MaxUsers" },
                values: new object[] { 18, 249m, 5000, 30, 150, 1000, 30, 26 });

            migrationBuilder.InsertData(
                table: "subscription_plans",
                columns: new[] { "Id", "BillingCycle", "CoinUsageDiscountPercent", "Cost", "CreatedAt", "Currency", "Features", "IsActive", "MaxAiCreditsMonthly", "MaxBrands", "MaxCampaignsMonthly", "MaxScheduledPosts", "MaxSocialAccounts", "MaxUsers", "Name" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000003"), "Monthly", 10, 99m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "USD", "[\"priority_support\"]", true, 1000, 10, 50, 200, 10, 9, "Plus" },
                    { new Guid("00000000-0000-0000-0000-000000000004"), "Monthly", 30, 599m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "USD", "[\"priority_support\",\"advanced_analytics\",\"dedicated_account_manager\"]", true, 20000, 100, 500, 5000, 100, 101, "Ultra" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_billing_transactions_TenantId_CreatedAt",
                table: "billing_transactions",
                columns: new[] { "TenantId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "billing_transactions");

            migrationBuilder.DropTable(
                name: "coin_packages");

            migrationBuilder.DeleteData(
                table: "subscription_plans",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "subscription_plans",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000004"));

            migrationBuilder.DropColumn(
                name: "ExtraBrandsPurchased",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "ExtraMarketeersPurchased",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "FreeContentGenerationsRemaining",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "FreeImageGenerationsRemaining",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "FreeMarketingPlanUsed",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "CoinUsageDiscountPercent",
                table: "subscription_plans");

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "MaxUsers",
                value: 3);

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                columns: new[] { "Cost", "MaxAiCreditsMonthly", "MaxBrands", "MaxCampaignsMonthly", "MaxScheduledPosts", "MaxSocialAccounts", "MaxUsers" },
                values: new object[] { 49.99m, 1000, 5, 50, 200, 10, 10 });
        }
    }
}
