using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rawaj.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveBrandVoiceIntoBrandInfoTones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BrandVoice",
                table: "tenant_brand_profiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BrandVoice",
                table: "tenant_brand_profiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }
    }
}
