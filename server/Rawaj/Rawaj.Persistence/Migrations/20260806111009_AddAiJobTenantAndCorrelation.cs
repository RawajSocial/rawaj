using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rawaj.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiJobTenantAndCorrelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ai_pipeline_stages_ai_jobs_AiJobId",
                table: "ai_pipeline_stages");

            migrationBuilder.DropIndex(
                name: "IX_ai_pipeline_stages_AiJobId",
                table: "ai_pipeline_stages");

            migrationBuilder.DropColumn(
                name: "AiJobId",
                table: "ai_pipeline_stages");

            migrationBuilder.AddColumn<int>(
                name: "LatencyMs",
                table: "ai_jobs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "ai_jobs",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PipelineStageId",
                table: "ai_jobs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromptHash",
                table: "ai_jobs",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "ai_jobs",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ai_jobs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Backfill, in two passes — without it every existing row keeps the Guid.Empty default
            // and the column is useless for exactly the historical reporting it was added for.
            //
            // Pass 1: rows that have a brand profile, which is how tenant attribution worked until
            // now.
            migrationBuilder.Sql("""
                UPDATE j
                SET j.TenantId = bp.TenantId
                FROM ai_jobs j
                INNER JOIN tenant_brand_profiles bp ON bp.Id = j.BrandProfileId
                WHERE j.BrandProfileId IS NOT NULL;
                """);

            // Pass 2: the trial generations (BrandProfileId is null since the
            // MakeAiJobBrandProfileOptional migration), which is precisely the set no per-tenant
            // query has ever been able to see. They are recoverable through the user who triggered
            // them: take that user's earliest tenant membership. Best-effort by nature — a user
            // belonging to several tenants can't be resolved exactly from a historical row, and one
            // belonging to none is left on Guid.Empty rather than guessed at. Both are acceptable
            // for backfilled history; every row written from here on carries the real tenant.
            migrationBuilder.Sql("""
                UPDATE j
                SET j.TenantId = m.TenantId
                FROM ai_jobs j
                CROSS APPLY (
                    SELECT TOP 1 tm.TenantId
                    FROM tenant_members tm
                    WHERE tm.UserId = j.TriggeredBy
                    ORDER BY tm.CreatedAt, tm.TenantId
                ) m
                WHERE j.TenantId = '00000000-0000-0000-0000-000000000000';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ai_jobs_PipelineStageId",
                table: "ai_jobs",
                column: "PipelineStageId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_jobs_TenantId_CreatedAt",
                table: "ai_jobs",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_ai_jobs_ai_pipeline_stages_PipelineStageId",
                table: "ai_jobs",
                column: "PipelineStageId",
                principalTable: "ai_pipeline_stages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ai_jobs_ai_pipeline_stages_PipelineStageId",
                table: "ai_jobs");

            migrationBuilder.DropIndex(
                name: "IX_ai_jobs_PipelineStageId",
                table: "ai_jobs");

            migrationBuilder.DropIndex(
                name: "IX_ai_jobs_TenantId_CreatedAt",
                table: "ai_jobs");

            migrationBuilder.DropColumn(
                name: "LatencyMs",
                table: "ai_jobs");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "ai_jobs");

            migrationBuilder.DropColumn(
                name: "PipelineStageId",
                table: "ai_jobs");

            migrationBuilder.DropColumn(
                name: "PromptHash",
                table: "ai_jobs");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "ai_jobs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ai_jobs");

            migrationBuilder.AddColumn<Guid>(
                name: "AiJobId",
                table: "ai_pipeline_stages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ai_pipeline_stages_AiJobId",
                table: "ai_pipeline_stages",
                column: "AiJobId");

            migrationBuilder.AddForeignKey(
                name: "FK_ai_pipeline_stages_ai_jobs_AiJobId",
                table: "ai_pipeline_stages",
                column: "AiJobId",
                principalTable: "ai_jobs",
                principalColumn: "Id");
        }
    }
}
