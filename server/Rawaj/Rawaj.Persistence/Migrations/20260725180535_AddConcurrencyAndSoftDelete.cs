using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rawaj.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConcurrencyAndSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "visual_assets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "visual_assets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "visual_assets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "tenant_brand_profiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "tenant_brand_profiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "tenant_brand_profiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "tenant_brand_profiles",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "marketing_campaigns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "marketing_campaigns",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "marketing_campaigns",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "marketing_campaigns",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "content_items",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "content_items",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "content_items",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "content_items",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "visual_assets");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "visual_assets");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "visual_assets");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "tenant_brand_profiles");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "tenant_brand_profiles");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "tenant_brand_profiles");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "tenant_brand_profiles");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "marketing_campaigns");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "marketing_campaigns");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "marketing_campaigns");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "marketing_campaigns");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "content_items");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "content_items");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "content_items");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "content_items");
        }
    }
}
