using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rawaj.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignPipelineRefs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrentBrandAnalysisArtifactId",
                table: "tenant_brand_profiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedStrategyArtifactId",
                table: "marketing_campaigns",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentPipelineRunId",
                table: "marketing_campaigns",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PipelineStageId",
                table: "content_items",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_brand_profiles_CurrentBrandAnalysisArtifactId",
                table: "tenant_brand_profiles",
                column: "CurrentBrandAnalysisArtifactId");

            migrationBuilder.CreateIndex(
                name: "IX_marketing_campaigns_ApprovedStrategyArtifactId",
                table: "marketing_campaigns",
                column: "ApprovedStrategyArtifactId");

            migrationBuilder.CreateIndex(
                name: "IX_marketing_campaigns_CurrentPipelineRunId",
                table: "marketing_campaigns",
                column: "CurrentPipelineRunId");

            migrationBuilder.CreateIndex(
                name: "IX_content_items_PipelineStageId",
                table: "content_items",
                column: "PipelineStageId");

            migrationBuilder.AddForeignKey(
                name: "FK_content_items_ai_pipeline_stages_PipelineStageId",
                table: "content_items",
                column: "PipelineStageId",
                principalTable: "ai_pipeline_stages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_marketing_campaigns_ai_artifacts_ApprovedStrategyArtifactId",
                table: "marketing_campaigns",
                column: "ApprovedStrategyArtifactId",
                principalTable: "ai_artifacts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_marketing_campaigns_ai_pipeline_runs_CurrentPipelineRunId",
                table: "marketing_campaigns",
                column: "CurrentPipelineRunId",
                principalTable: "ai_pipeline_runs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_tenant_brand_profiles_ai_artifacts_CurrentBrandAnalysisArtifactId",
                table: "tenant_brand_profiles",
                column: "CurrentBrandAnalysisArtifactId",
                principalTable: "ai_artifacts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_content_items_ai_pipeline_stages_PipelineStageId",
                table: "content_items");

            migrationBuilder.DropForeignKey(
                name: "FK_marketing_campaigns_ai_artifacts_ApprovedStrategyArtifactId",
                table: "marketing_campaigns");

            migrationBuilder.DropForeignKey(
                name: "FK_marketing_campaigns_ai_pipeline_runs_CurrentPipelineRunId",
                table: "marketing_campaigns");

            migrationBuilder.DropForeignKey(
                name: "FK_tenant_brand_profiles_ai_artifacts_CurrentBrandAnalysisArtifactId",
                table: "tenant_brand_profiles");

            migrationBuilder.DropIndex(
                name: "IX_tenant_brand_profiles_CurrentBrandAnalysisArtifactId",
                table: "tenant_brand_profiles");

            migrationBuilder.DropIndex(
                name: "IX_marketing_campaigns_ApprovedStrategyArtifactId",
                table: "marketing_campaigns");

            migrationBuilder.DropIndex(
                name: "IX_marketing_campaigns_CurrentPipelineRunId",
                table: "marketing_campaigns");

            migrationBuilder.DropIndex(
                name: "IX_content_items_PipelineStageId",
                table: "content_items");

            migrationBuilder.DropColumn(
                name: "CurrentBrandAnalysisArtifactId",
                table: "tenant_brand_profiles");

            migrationBuilder.DropColumn(
                name: "ApprovedStrategyArtifactId",
                table: "marketing_campaigns");

            migrationBuilder.DropColumn(
                name: "CurrentPipelineRunId",
                table: "marketing_campaigns");

            migrationBuilder.DropColumn(
                name: "PipelineStageId",
                table: "content_items");
        }
    }
}
