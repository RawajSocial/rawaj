using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rawaj.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledPostBrandCampaignScoping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BrandProfileId",
                table: "scheduled_posts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CampaignId",
                table: "scheduled_posts",
                type: "uniqueidentifier",
                nullable: true);

            // Backfill the new columns for pre-existing rows before the FK constraints below are
            // added (the AddColumn default of an all-zero Guid above would otherwise violate the
            // BrandProfileId FK for any row that already existed). BrandProfileId prefers the
            // content item's brand (content can outlive/precede a campaign) and falls back to the
            // social account's brand if the content item somehow has none; CampaignId comes from
            // the content item only, since a post has no campaign of its own outside of that.
            migrationBuilder.Sql(
                """
                UPDATE sp
                SET sp.BrandProfileId = COALESCE(ci.BrandProfileId, sa.BrandProfileId),
                    sp.CampaignId = ci.CampaignId
                FROM scheduled_posts sp
                JOIN content_items ci ON sp.ContentItemId = ci.Id
                JOIN social_accounts sa ON sp.SocialAccountId = sa.Id;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_posts_BrandProfileId",
                table: "scheduled_posts",
                column: "BrandProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_posts_CampaignId",
                table: "scheduled_posts",
                column: "CampaignId");

            migrationBuilder.AddForeignKey(
                name: "FK_scheduled_posts_marketing_campaigns_CampaignId",
                table: "scheduled_posts",
                column: "CampaignId",
                principalTable: "marketing_campaigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_scheduled_posts_tenant_brand_profiles_BrandProfileId",
                table: "scheduled_posts",
                column: "BrandProfileId",
                principalTable: "tenant_brand_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_scheduled_posts_marketing_campaigns_CampaignId",
                table: "scheduled_posts");

            migrationBuilder.DropForeignKey(
                name: "FK_scheduled_posts_tenant_brand_profiles_BrandProfileId",
                table: "scheduled_posts");

            migrationBuilder.DropIndex(
                name: "IX_scheduled_posts_BrandProfileId",
                table: "scheduled_posts");

            migrationBuilder.DropIndex(
                name: "IX_scheduled_posts_CampaignId",
                table: "scheduled_posts");

            migrationBuilder.DropColumn(
                name: "BrandProfileId",
                table: "scheduled_posts");

            migrationBuilder.DropColumn(
                name: "CampaignId",
                table: "scheduled_posts");
        }
    }
}
