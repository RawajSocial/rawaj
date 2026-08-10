using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rawaj.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenamePostAnalyticsMetricsAndAddSnapshotIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_post_analytics_ScheduledPostId",
                table: "post_analytics");

            migrationBuilder.RenameColumn(
                name: "Impressions",
                table: "post_analytics",
                newName: "Views");

            migrationBuilder.RenameColumn(
                name: "Reach",
                table: "post_analytics",
                newName: "UniqueViewers");

            migrationBuilder.CreateIndex(
                name: "IX_post_analytics_ScheduledPostId_RecordedAt",
                table: "post_analytics",
                columns: new[] { "ScheduledPostId", "RecordedAt" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_post_analytics_ScheduledPostId_RecordedAt",
                table: "post_analytics");

            migrationBuilder.RenameColumn(
                name: "Views",
                table: "post_analytics",
                newName: "Impressions");

            migrationBuilder.RenameColumn(
                name: "UniqueViewers",
                table: "post_analytics",
                newName: "Reach");

            migrationBuilder.CreateIndex(
                name: "IX_post_analytics_ScheduledPostId",
                table: "post_analytics",
                column: "ScheduledPostId");
        }
    }
}
