using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rawaj.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGenerationModeAndOptionalBrand : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "BrandProfileId",
                table: "visual_assets",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            // Every pre-existing row required a non-null BrandProfileId, so none of them are really
            // "Standalone" — default to "Brand" and then correct the ones that also have a
            // CampaignId to "Campaign" below, instead of leaving an invalid/blank enum value that
            // would fail to parse back into GenerationMode on read.
            migrationBuilder.AddColumn<string>(
                name: "GenerationMode",
                table: "visual_assets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Brand");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "visual_assets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GenerationMode",
                table: "content_items",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Brand");

            migrationBuilder.Sql(
                "UPDATE visual_assets SET GenerationMode = 'Campaign' WHERE CampaignId IS NOT NULL;");
            migrationBuilder.Sql(
                "UPDATE content_items SET GenerationMode = 'Campaign' WHERE CampaignId IS NOT NULL;");

            // Backfill TenantId on visual_assets from the brand it belongs to, so ReviewVisualAsset's
            // (and any future) tenant-scoped lookups keep working for rows created before this column
            // existed — going forward, new rows always set TenantId directly at creation time.
            migrationBuilder.Sql(
                "UPDATE va SET va.TenantId = bp.TenantId FROM visual_assets va " +
                "INNER JOIN tenant_brand_profiles bp ON bp.Id = va.BrandProfileId WHERE va.BrandProfileId IS NOT NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_visual_assets_TenantId",
                table: "visual_assets",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_visual_assets_tenants_TenantId",
                table: "visual_assets",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_visual_assets_tenants_TenantId",
                table: "visual_assets");

            migrationBuilder.DropIndex(
                name: "IX_visual_assets_TenantId",
                table: "visual_assets");

            migrationBuilder.DropColumn(
                name: "GenerationMode",
                table: "visual_assets");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "visual_assets");

            migrationBuilder.DropColumn(
                name: "GenerationMode",
                table: "content_items");

            migrationBuilder.AlterColumn<Guid>(
                name: "BrandProfileId",
                table: "visual_assets",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
