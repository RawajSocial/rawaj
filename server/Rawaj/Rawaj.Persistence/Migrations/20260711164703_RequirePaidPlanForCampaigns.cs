using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rawaj.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RequirePaidPlanForCampaigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "MaxCampaignsMonthly",
                value: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "MaxCampaignsMonthly",
                value: 5);
        }
    }
}
