using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rawaj.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase8Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_scheduled_posts_BrandProfileId",
                table: "scheduled_posts");

            migrationBuilder.DropIndex(
                name: "IX_notifications_UserId",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_marketing_campaigns_BrandProfileId",
                table: "marketing_campaigns");

            migrationBuilder.DropIndex(
                name: "IX_logs_TenantId",
                table: "logs");

            migrationBuilder.DropIndex(
                name: "IX_content_items_BrandProfileId",
                table: "content_items");

            migrationBuilder.DropIndex(
                name: "IX_content_items_TenantId",
                table: "content_items");

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_posts_BrandProfileId_ScheduledAt",
                table: "scheduled_posts",
                columns: new[] { "BrandProfileId", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_posts_Status_ScheduledAt",
                table: "scheduled_posts",
                columns: new[] { "Status", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_UserId_IsRead",
                table: "notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_marketing_campaigns_BrandProfileId_Status",
                table: "marketing_campaigns",
                columns: new[] { "BrandProfileId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_logs_TenantId_CreatedAt",
                table: "logs",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_content_items_BrandProfileId_Status",
                table: "content_items",
                columns: new[] { "BrandProfileId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_content_items_TenantId_Status",
                table: "content_items",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_scheduled_posts_BrandProfileId_ScheduledAt",
                table: "scheduled_posts");

            migrationBuilder.DropIndex(
                name: "IX_scheduled_posts_Status_ScheduledAt",
                table: "scheduled_posts");

            migrationBuilder.DropIndex(
                name: "IX_notifications_UserId_IsRead",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_marketing_campaigns_BrandProfileId_Status",
                table: "marketing_campaigns");

            migrationBuilder.DropIndex(
                name: "IX_logs_TenantId_CreatedAt",
                table: "logs");

            migrationBuilder.DropIndex(
                name: "IX_content_items_BrandProfileId_Status",
                table: "content_items");

            migrationBuilder.DropIndex(
                name: "IX_content_items_TenantId_Status",
                table: "content_items");

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_posts_BrandProfileId",
                table: "scheduled_posts",
                column: "BrandProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_UserId",
                table: "notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_marketing_campaigns_BrandProfileId",
                table: "marketing_campaigns",
                column: "BrandProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_logs_TenantId",
                table: "logs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_content_items_BrandProfileId",
                table: "content_items",
                column: "BrandProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_content_items_TenantId",
                table: "content_items",
                column: "TenantId");
        }
    }
}
