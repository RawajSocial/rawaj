using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rawaj.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackfillPipelineRunsFromExistingCampaigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Synthesises a Completed (or AwaitingApproval, if never approved) pipeline run for every
            // campaign that already has a strategy, so old campaigns render as "already run" instead
            // of "never run" once C19 cuts reads over to the new run/stage model. Only the strategy
            // half of the graph is modelled — ContentPlan/ContentImage are left out deliberately,
            // since content generation's own output (ContentItem/VisualAsset rows) is untouched by
            // this plan and was never tied to "a run" even conceptually until now.
            //
            // Two of the nine strategy-graph stages (BrandAnalysis, CampaignAnalysis) and the three
            // intermediate strategy sub-stages (StrategyPositioning/Blueprint/Roadmap) are marked
            // Completed with no backing artifact: DiagnosisJson only ever held the union of what
            // those five stages produce, not each stage's own shape, so there is nothing to
            // reconstruct them from. Only the two artifacts a legacy column already names directly —
            // the assembled Strategy (from AiPlanJson) and CompetitorResearch (from
            // CompetitorResearchJson, when it actually found something) — are backfilled for real.
            //
            // Idempotent by construction: re-running only ever touches campaigns whose
            // CurrentPipelineRunId is still null, so a second application of this migration (or a
            // campaign created for the first time by C19's live pipeline before this ran) does nothing.
            migrationBuilder.Sql("""
                CREATE TABLE #backfill (
                    CampaignId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    BrandProfileId UNIQUEIDENTIFIER NOT NULL,
                    TenantId UNIQUEIDENTIFIER NOT NULL,
                    TriggeredBy UNIQUEIDENTIFIER NOT NULL,
                    RunId UNIQUEIDENTIFIER NOT NULL,
                    StrategyArtifactId UNIQUEIDENTIFIER NOT NULL,
                    CompetitorArtifactId UNIQUEIDENTIFIER NOT NULL,
                    CompetitorAvailable BIT NOT NULL,
                    Approved BIT NOT NULL,
                    StartedAt DATETIME2 NOT NULL,
                    SettledAt DATETIME2 NOT NULL
                );

                INSERT INTO #backfill (
                    CampaignId, BrandProfileId, TenantId, TriggeredBy, RunId, StrategyArtifactId,
                    CompetitorArtifactId, CompetitorAvailable, Approved, StartedAt, SettledAt)
                SELECT
                    c.Id,
                    c.BrandProfileId,
                    bp.TenantId,
                    c.CreatedBy,
                    NEWID(),
                    NEWID(),
                    NEWID(),
                    CASE WHEN c.CompetitorResearchJson IS NOT NULL
                              AND ISJSON(c.CompetitorResearchJson) = 1
                              AND JSON_VALUE(c.CompetitorResearchJson, '$.unavailable') = 'false'
                         THEN 1 ELSE 0 END,
                    CASE WHEN c.PlanApprovedAt IS NOT NULL THEN 1 ELSE 0 END,
                    COALESCE(c.AiGeneratedAt, c.CreatedAt),
                    COALESCE(c.PlanApprovedAt, c.AiGeneratedAt, c.UpdatedAt)
                FROM marketing_campaigns c
                INNER JOIN tenant_brand_profiles bp ON bp.Id = c.BrandProfileId
                WHERE c.AiPlanJson IS NOT NULL
                  AND ISJSON(c.AiPlanJson) = 1
                  AND c.CurrentPipelineRunId IS NULL;

                INSERT INTO ai_pipeline_runs (
                    Id, TenantId, BrandProfileId, CampaignId, TriggeredBy, Status, CurrentStage,
                    TotalCoinsSpent, LastError, StartedAt, CompletedAt, CreatedAt, UpdatedAt)
                SELECT
                    b.RunId, b.TenantId, b.BrandProfileId, b.CampaignId, b.TriggeredBy,
                    CASE WHEN b.Approved = 1 THEN 'Completed' ELSE 'AwaitingApproval' END,
                    'HumanApproval',
                    0,
                    NULL,
                    b.StartedAt,
                    CASE WHEN b.Approved = 1 THEN b.SettledAt ELSE NULL END,
                    b.StartedAt,
                    b.SettledAt
                FROM #backfill b;

                INSERT INTO ai_artifacts (
                    Id, TenantId, BrandProfileId, CampaignId, Kind, Version, IsCurrent, ContentJson,
                    SchemaVersion, SourceStageId, InputHash, CreatedAt)
                SELECT
                    b.StrategyArtifactId, b.TenantId, b.BrandProfileId, b.CampaignId, 'Strategy', 1, 1,
                    c.AiPlanJson, 1, NULL, NULL, b.StartedAt
                FROM #backfill b
                INNER JOIN marketing_campaigns c ON c.Id = b.CampaignId;

                INSERT INTO ai_artifacts (
                    Id, TenantId, BrandProfileId, CampaignId, Kind, Version, IsCurrent, ContentJson,
                    SchemaVersion, SourceStageId, InputHash, CreatedAt)
                SELECT
                    b.CompetitorArtifactId, b.TenantId, b.BrandProfileId, b.CampaignId,
                    'CompetitorResearch', 1, 1, c.CompetitorResearchJson, 1, NULL, NULL, b.StartedAt
                FROM #backfill b
                INNER JOIN marketing_campaigns c ON c.Id = b.CampaignId
                WHERE b.CompetitorAvailable = 1;

                -- BrandAnalysis, CampaignAnalysis, StrategyPositioning, StrategyBlueprint,
                -- StrategyRoadmap: Completed, no reconstructable artifact (see comment above).
                INSERT INTO ai_pipeline_stages (
                    Id, RunId, Kind, Ordinal, Status, IsOptional, Attempts, MaxAttempts, NextAttemptAt,
                    LeaseOwner, LeaseExpiresAt, InputHash, ArtifactId, CoinsCharged, LastError,
                    LastErrorKind, TargetRefId, TargetRefType, StartedAt, CompletedAt, CreatedAt)
                SELECT NEWID(), b.RunId, k.Kind, k.Ordinal, 'Completed', 0, 1, 3, NULL, NULL, NULL, NULL,
                       NULL, 0, NULL, NULL, NULL, NULL, b.StartedAt, b.StartedAt, b.StartedAt
                FROM #backfill b
                CROSS JOIN (VALUES
                    ('BrandAnalysis', 10),
                    ('CampaignAnalysis', 20),
                    ('StrategyPositioning', 50),
                    ('StrategyBlueprint', 60),
                    ('StrategyRoadmap', 70)
                ) AS k(Kind, Ordinal);

                -- MarketResearch: no legacy column ever held this, so there is nothing to know it
                -- succeeded from — Skipped, consistent with "optional stage exhausted" semantics.
                INSERT INTO ai_pipeline_stages (
                    Id, RunId, Kind, Ordinal, Status, IsOptional, Attempts, MaxAttempts, NextAttemptAt,
                    LeaseOwner, LeaseExpiresAt, InputHash, ArtifactId, CoinsCharged, LastError,
                    LastErrorKind, TargetRefId, TargetRefType, StartedAt, CompletedAt, CreatedAt)
                SELECT NEWID(), b.RunId, 'MarketResearch', 30, 'Skipped', 1, 3, 3, NULL, NULL, NULL,
                       NULL, NULL, 0, NULL, NULL, NULL, NULL, b.StartedAt, b.StartedAt, b.StartedAt
                FROM #backfill b;

                INSERT INTO ai_pipeline_stages (
                    Id, RunId, Kind, Ordinal, Status, IsOptional, Attempts, MaxAttempts, NextAttemptAt,
                    LeaseOwner, LeaseExpiresAt, InputHash, ArtifactId, CoinsCharged, LastError,
                    LastErrorKind, TargetRefId, TargetRefType, StartedAt, CompletedAt, CreatedAt)
                SELECT NEWID(), b.RunId, 'CompetitorResearch', 40,
                       CASE WHEN b.CompetitorAvailable = 1 THEN 'Completed' ELSE 'Skipped' END,
                       1,
                       CASE WHEN b.CompetitorAvailable = 1 THEN 1 ELSE 3 END,
                       3, NULL, NULL, NULL, NULL,
                       CASE WHEN b.CompetitorAvailable = 1 THEN b.CompetitorArtifactId ELSE NULL END,
                       0, NULL, NULL, NULL, NULL, b.StartedAt, b.StartedAt, b.StartedAt
                FROM #backfill b;

                INSERT INTO ai_pipeline_stages (
                    Id, RunId, Kind, Ordinal, Status, IsOptional, Attempts, MaxAttempts, NextAttemptAt,
                    LeaseOwner, LeaseExpiresAt, InputHash, ArtifactId, CoinsCharged, LastError,
                    LastErrorKind, TargetRefId, TargetRefType, StartedAt, CompletedAt, CreatedAt)
                SELECT NEWID(), b.RunId, 'StrategyAssemble', 80, 'Completed', 0, 1, 3, NULL, NULL, NULL,
                       NULL, b.StrategyArtifactId, 0, NULL, NULL, NULL, NULL, b.StartedAt, b.StartedAt,
                       b.StartedAt
                FROM #backfill b;

                INSERT INTO ai_pipeline_stages (
                    Id, RunId, Kind, Ordinal, Status, IsOptional, Attempts, MaxAttempts, NextAttemptAt,
                    LeaseOwner, LeaseExpiresAt, InputHash, ArtifactId, CoinsCharged, LastError,
                    LastErrorKind, TargetRefId, TargetRefType, StartedAt, CompletedAt, CreatedAt)
                SELECT NEWID(), b.RunId, 'HumanApproval', 90,
                       CASE WHEN b.Approved = 1 THEN 'Completed' ELSE 'AwaitingApproval' END,
                       0,
                       CASE WHEN b.Approved = 1 THEN 1 ELSE 0 END,
                       1, NULL, NULL, NULL, NULL,
                       CASE WHEN b.Approved = 1 THEN b.StrategyArtifactId ELSE NULL END,
                       0, NULL, NULL, NULL, NULL, b.StartedAt,
                       CASE WHEN b.Approved = 1 THEN b.SettledAt ELSE NULL END,
                       b.StartedAt
                FROM #backfill b;

                UPDATE c
                SET c.CurrentPipelineRunId = b.RunId,
                    c.ApprovedStrategyArtifactId = CASE WHEN b.Approved = 1 THEN b.StrategyArtifactId ELSE NULL END
                FROM marketing_campaigns c
                INNER JOIN #backfill b ON b.CampaignId = c.Id;

                DROP TABLE #backfill;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data-only backfill, no schema change — matching AddAiJobTenantAndCorrelation's
            // convention, this is not reversed. The synthesised rows are historical best-effort
            // reconstructions, not new information whose loss on rollback would matter; leaving them
            // in place is strictly safer than guessing which runs a rollback should delete.
        }
    }
}
