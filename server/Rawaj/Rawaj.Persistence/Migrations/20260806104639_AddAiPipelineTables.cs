using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rawaj.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiPipelineTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_pipeline_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BrandProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TriggeredBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CurrentStage = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    TotalCoinsSpent = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_pipeline_runs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_pipeline_runs_marketing_campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "marketing_campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_pipeline_runs_tenant_brand_profiles_BrandProfileId",
                        column: x => x.BrandProfileId,
                        principalTable: "tenant_brand_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_pipeline_runs_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_pipeline_runs_users_TriggeredBy",
                        column: x => x.TriggeredBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ai_artifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BrandProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Kind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    ContentJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SchemaVersion = table.Column<int>(type: "int", nullable: false),
                    SourceStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InputHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_artifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_artifacts_marketing_campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "marketing_campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_artifacts_tenant_brand_profiles_BrandProfileId",
                        column: x => x.BrandProfileId,
                        principalTable: "tenant_brand_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_artifacts_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ai_pipeline_stages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsOptional = table.Column<bool>(type: "bit", nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    MaxAttempts = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LeaseOwner = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LeaseExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InputHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ArtifactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CoinsCharged = table.Column<int>(type: "int", nullable: false),
                    AiJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastErrorKind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TargetRefId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetRefType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_pipeline_stages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_pipeline_stages_ai_artifacts_ArtifactId",
                        column: x => x.ArtifactId,
                        principalTable: "ai_artifacts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ai_pipeline_stages_ai_jobs_AiJobId",
                        column: x => x.AiJobId,
                        principalTable: "ai_jobs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ai_pipeline_stages_ai_pipeline_runs_RunId",
                        column: x => x.RunId,
                        principalTable: "ai_pipeline_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_artifacts_BrandProfileId_Kind_InputHash",
                table: "ai_artifacts",
                columns: new[] { "BrandProfileId", "Kind", "InputHash" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_artifacts_CampaignId_Kind_IsCurrent",
                table: "ai_artifacts",
                columns: new[] { "CampaignId", "Kind", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_artifacts_CampaignId_Kind_Version",
                table: "ai_artifacts",
                columns: new[] { "CampaignId", "Kind", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_artifacts_SourceStageId",
                table: "ai_artifacts",
                column: "SourceStageId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_artifacts_TenantId",
                table: "ai_artifacts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_pipeline_runs_BrandProfileId",
                table: "ai_pipeline_runs",
                column: "BrandProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_pipeline_runs_CampaignId_CreatedAt",
                table: "ai_pipeline_runs",
                columns: new[] { "CampaignId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_pipeline_runs_Status_Active",
                table: "ai_pipeline_runs",
                column: "Status",
                filter: "[Status] IN ('Pending', 'Running')");

            migrationBuilder.CreateIndex(
                name: "IX_ai_pipeline_runs_TenantId_Status",
                table: "ai_pipeline_runs",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_pipeline_runs_TriggeredBy",
                table: "ai_pipeline_runs",
                column: "TriggeredBy");

            migrationBuilder.CreateIndex(
                name: "IX_ai_pipeline_stages_AiJobId",
                table: "ai_pipeline_stages",
                column: "AiJobId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_pipeline_stages_ArtifactId",
                table: "ai_pipeline_stages",
                column: "ArtifactId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_pipeline_stages_Pending",
                table: "ai_pipeline_stages",
                columns: new[] { "Status", "NextAttemptAt" },
                filter: "[Status] = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_ai_pipeline_stages_RunId_Kind_TargetRefId",
                table: "ai_pipeline_stages",
                columns: new[] { "RunId", "Kind", "TargetRefId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ai_pipeline_stages_Running_Lease",
                table: "ai_pipeline_stages",
                columns: new[] { "Status", "LeaseExpiresAt" },
                filter: "[Status] = 'Running'");

            migrationBuilder.AddForeignKey(
                name: "FK_ai_artifacts_ai_pipeline_stages_SourceStageId",
                table: "ai_artifacts",
                column: "SourceStageId",
                principalTable: "ai_pipeline_stages",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ai_artifacts_ai_pipeline_stages_SourceStageId",
                table: "ai_artifacts");

            migrationBuilder.DropTable(
                name: "ai_pipeline_stages");

            migrationBuilder.DropTable(
                name: "ai_artifacts");

            migrationBuilder.DropTable(
                name: "ai_pipeline_runs");
        }
    }
}
