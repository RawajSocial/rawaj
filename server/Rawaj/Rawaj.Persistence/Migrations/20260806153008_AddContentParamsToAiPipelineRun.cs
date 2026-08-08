using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rawaj.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContentParamsToAiPipelineRun : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Defaults match AiPipelineRun's C# property defaults, not EF's usual
            // false/""/0-for-the-CLR-type — every run created before this migration (including the
            // ones the previous migration just backfilled) gets the same content-generation defaults
            // ContentPlanExecutor's now-removed placeholder constants used to hardcode.
            migrationBuilder.AddColumn<bool>(
                name: "ContentIncludeImages",
                table: "ai_pipeline_runs",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentLanguage",
                table: "ai_pipeline_runs",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "Ar");

            migrationBuilder.AddColumn<int>(
                name: "ContentPostCount",
                table: "ai_pipeline_runs",
                type: "int",
                nullable: false,
                defaultValue: 8);

            migrationBuilder.AddColumn<string>(
                name: "ContentTemplateStyle",
                table: "ai_pipeline_runs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Auto");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentIncludeImages",
                table: "ai_pipeline_runs");

            migrationBuilder.DropColumn(
                name: "ContentLanguage",
                table: "ai_pipeline_runs");

            migrationBuilder.DropColumn(
                name: "ContentPostCount",
                table: "ai_pipeline_runs");

            migrationBuilder.DropColumn(
                name: "ContentTemplateStyle",
                table: "ai_pipeline_runs");
        }
    }
}
