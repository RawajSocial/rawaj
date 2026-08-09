using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rawaj.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFollowerCountAndPostAnalyticsSyncError : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FollowerCount",
                table: "social_accounts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FollowerCountSyncedAt",
                table: "social_accounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SyncError",
                table: "post_analytics",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FollowerCount",
                table: "social_accounts");

            migrationBuilder.DropColumn(
                name: "FollowerCountSyncedAt",
                table: "social_accounts");

            migrationBuilder.DropColumn(
                name: "SyncError",
                table: "post_analytics");
        }
    }
}
