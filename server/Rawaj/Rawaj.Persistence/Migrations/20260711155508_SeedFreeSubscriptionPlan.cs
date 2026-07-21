using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rawaj.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedFreeSubscriptionPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "subscription_plans",
                columns: new[] { "Id", "BillingCycle", "Cost", "CreatedAt", "Currency", "Features", "IsActive", "MaxAiCreditsMonthly", "MaxBrands", "MaxCampaignsMonthly", "MaxScheduledPosts", "MaxSocialAccounts", "MaxUsers", "Name" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), "Monthly", 0m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "USD", "[]", true, 50, 1, 5, 10, 2, 1, "Free" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "subscription_plans",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"));
        }
    }
}
