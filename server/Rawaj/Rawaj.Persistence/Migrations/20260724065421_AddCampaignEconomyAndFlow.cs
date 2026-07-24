using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rawaj.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignEconomyAndFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastCoinGrantAt",
                table: "subscriptions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MonthlyCoinGrant",
                table: "subscription_plans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BriefJson",
                table: "marketing_campaigns",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompetitorResearchJson",
                table: "marketing_campaigns",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiagnosisJson",
                table: "marketing_campaigns",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlanApprovedAt",
                table: "marketing_campaigns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "MaxCampaignsMonthly", "MaxUsers", "MonthlyCoinGrant" },
                values: new object[] { 1, 2, 100 });

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "MonthlyCoinGrant",
                value: 30000);

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"),
                column: "MonthlyCoinGrant",
                value: 10000);

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000004"),
                column: "MonthlyCoinGrant",
                value: 80000);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastCoinGrantAt",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "MonthlyCoinGrant",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "BriefJson",
                table: "marketing_campaigns");

            migrationBuilder.DropColumn(
                name: "CompetitorResearchJson",
                table: "marketing_campaigns");

            migrationBuilder.DropColumn(
                name: "DiagnosisJson",
                table: "marketing_campaigns");

            migrationBuilder.DropColumn(
                name: "PlanApprovedAt",
                table: "marketing_campaigns");

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "MaxCampaignsMonthly", "MaxUsers" },
                values: new object[] { 0, 1 });
        }
    }
}
