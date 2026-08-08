using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Features.AiPipeline.Services;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.Common;

/// <summary>
/// C19 cutover glue for the legacy campaign endpoints that each used to make exactly one AI call of
/// their own (research-competitors, diagnose-business, generate-plan, refine-plan): gets the
/// campaign's pipeline run going and advances it, so the caller only has to read the one legacy
/// column it cares about back off <c>MarketingCampaign</c> afterwards — C18's write-through is what
/// makes that column trustworthy.
///
/// <para>Not used by generate-content: that endpoint supports being called repeatedly to add another
/// batch of posts to an already-content-having campaign, which the pipeline can't do yet (one
/// <c>ContentPlan</c> row per run, and a run always starts from <c>BrandAnalysis</c>) — it stays on
/// its own handler until a follow-up commit adds a content-only run.</para>
/// </summary>
public static class LegacyPipelineGateway
{
    /// <summary>Loads (or starts) the campaign's current run and advances it as far as it will go
    /// right now. Safe to call from any of the four shims regardless of which one the frontend
    /// happens to call first — whichever gets there first does the actual work, since one
    /// <c>AdvanceAsync</c> runs everything currently runnable, not just one stage.</summary>
    public static async Task<AiPipelineRun> EnsureRunAdvancedAsync(
        IApplicationDbContext dbContext,
        IPipelineOrchestrator orchestrator,
        MarketingCampaign campaign,
        TenantBrandProfile brand,
        Guid triggeredBy,
        TenantMemberRole role,
        CancellationToken cancellationToken)
    {
        AiPipelineRun? run = campaign.CurrentPipelineRunId is { } runId
            ? await dbContext.AiPipelineRuns.FirstOrDefaultAsync(r => r.Id == runId, cancellationToken)
            : null;

        // A cancelled or failed run can't make further progress. The legacy endpoints never had a
        // concept of "start over" — calling one again is the closest thing to that intent.
        if (run is null || run.Status is AiPipelineRunStatus.Cancelled or AiPipelineRunStatus.Failed)
        {
            run = await orchestrator.StartAsync(brand.TenantId, brand.Id, campaign.Id, triggeredBy, cancellationToken);
            campaign.CurrentPipelineRunId = run.Id;
        }

        await orchestrator.AdvanceAsync(run, role, cancellationToken);
        return run;
    }

    /// <summary>Why the legacy column a shim wanted is still empty: the stage that would have
    /// produced it is short on coins, or a required stage upstream of it failed outright. Null means
    /// the run genuinely hasn't got there yet (nothing to report — the caller should fall back to a
    /// generic "try again" message).</summary>
    public static async Task<string?> BlockingErrorAsync(
        IApplicationDbContext dbContext, Guid runId, CancellationToken cancellationToken)
    {
        var stages = await dbContext.AiPipelineStages
            .Where(s => s.RunId == runId)
            .ToListAsync(cancellationToken);

        return stages.FirstOrDefault(s => s.LastErrorKind == AiFailureKind.Coins)?.LastError
            ?? stages.FirstOrDefault(s => s.Status == AiPipelineStageStatus.Failed && s.LastError is not null)?.LastError;
    }
}
