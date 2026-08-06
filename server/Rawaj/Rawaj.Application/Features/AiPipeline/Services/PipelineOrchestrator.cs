using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Policies;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.Services;

/// <summary>
/// Runs the graph a stage at a time, replacing what today is a chain of `subscribe()` callbacks
/// hand-wired in the browser — where a closed tab strands a campaign mid-generation with coins spent
/// and no artifact.
///
/// <para>Owns exactly the six things <c>IPipelineStageExecutor</c> deliberately doesn't: deciding
/// which stages are runnable, resolving a stage's inputs from the artifact store, dispatching to the
/// right executor, writing the artifact, charging (idempotently, and only once per usable result),
/// and deciding the run's next status. A stage never calls the next one — this is what decides what
/// happens next, every time.</para>
///
/// <para><b>Synchronous by design.</b> This is the graph-walking logic in isolation, driven directly
/// rather than through a hosted service and a queue — that split is C16's job (concurrency, leases,
/// bounded parallelism per provider, crash recovery via lease expiry). Everything here already
/// behaves correctly under that later change: a stage this method dispatches either completes or
/// fails in the same call, so there is nothing to reclaim from a synchronous run that never leaves a
/// stage half-finished.</para>
/// </summary>
public class PipelineOrchestrator(
    IApplicationDbContext dbContext,
    IPipelineArtifactStore artifacts,
    ICoinCostProvider coinCosts,
    IEnumerable<IPipelineStageExecutor> executors) : IPipelineOrchestrator
{
    private static readonly AiArtifactKind[] AllArtifactKinds = Enum.GetValues<AiArtifactKind>();

    private readonly Dictionary<AiPipelineStageKind, IPipelineStageExecutor> _executorsByKind =
        executors.ToDictionary(e => e.Kind);

    public async Task<AiPipelineRun> StartAsync(
        Guid tenantId, Guid brandProfileId, Guid? campaignId, Guid triggeredBy, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var run = new AiPipelineRun
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BrandProfileId = brandProfileId,
            CampaignId = campaignId,
            TriggeredBy = triggeredBy,
            Status = AiPipelineRunStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.AiPipelineRuns.Add(run);

        // A brand-only run (computing a brand analysis outside any campaign) has nothing for the
        // campaign-scoped stages to run against — starting the full graph would just burn three
        // attempts each on CampaignAnalysis and StrategyPositioning failing to find a campaign, and
        // fail the run over work it was never asked to do.
        var initialStages = campaignId is null
            ? AiPipelinePolicy.InitialStages().Where(d => d.Kind == AiPipelineStageKind.BrandAnalysis)
            : AiPipelinePolicy.InitialStages();

        foreach (var definition in initialStages)
        {
            dbContext.AiPipelineStages.Add(new AiPipelineStage
            {
                Id = Guid.NewGuid(),
                RunId = run.Id,
                Kind = definition.Kind,
                Ordinal = definition.Ordinal,
                Status = AiPipelineStageStatus.Pending,
                IsOptional = definition.IsOptional,
                MaxAttempts = definition.MaxAttempts,
                CreatedAt = now
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return run;
    }

    public async Task AdvanceAsync(AiPipelineRun run, TenantMemberRole role, CancellationToken cancellationToken)
    {
        while (true)
        {
            var stages = await LoadStagesAsync(run.Id, cancellationToken);
            var runnable = AiPipelinePolicy.GetRunnableStages(stages, DateTime.UtcNow);

            if (runnable.Count == 0)
            {
                break;
            }

            foreach (var stage in runnable)
            {
                if (AiPipelinePolicy.Definition(stage.Kind).RequiresHuman)
                {
                    // Never dispatched — the graph parks here until PipelineApprovalService clears it.
                    stage.Status = AiPipelineStageStatus.AwaitingApproval;
                    await dbContext.SaveChangesAsync(cancellationToken);
                    continue;
                }

                await ExecuteStageAsync(run, stage, role, cancellationToken);
            }

            run.Status = AiPipelinePolicy.EvaluateRunStatus(await LoadStagesAsync(run.Id, cancellationToken));
            run.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            // Anything other than Running means the run cannot make further progress on its own right
            // now — parked on a person, a wallet, time (backoff), or finished one way or another.
            // Looping again would either do nothing (nothing newly runnable) or, for a coins shortfall
            // specifically, spin forever: that stage stays Pending with no NextAttemptAt, so it would
            // be selected again immediately.
            if (run.Status != AiPipelineRunStatus.Running)
            {
                break;
            }
        }
    }

    public Task CancelAsync(AiPipelineRun run, CancellationToken cancellationToken)
    {
        run.Status = AiPipelineRunStatus.Cancelled;
        run.UpdatedAt = DateTime.UtcNow;
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    private Task<List<AiPipelineStage>> LoadStagesAsync(Guid runId, CancellationToken cancellationToken) =>
        dbContext.AiPipelineStages.Where(s => s.RunId == runId).ToListAsync(cancellationToken);

    private async Task ExecuteStageAsync(
        AiPipelineRun run, AiPipelineStage stage, TenantMemberRole role, CancellationToken cancellationToken)
    {
        var brand = await dbContext.TenantBrandProfiles.FirstAsync(b => b.Id == run.BrandProfileId, cancellationToken);
        var campaign = run.CampaignId is null
            ? null
            : await dbContext.MarketingCampaigns.FirstAsync(c => c.Id == run.CampaignId, cancellationToken);

        // Checked before the provider is ever called, not after — a tenant who cannot afford this
        // stage is parked without spending anything, rather than charged for work that then can't be
        // paid for.
        if (AiPipelineCoinPolicy.IsChargePoint(stage.Kind))
        {
            var charge = await AiPipelineCoinPolicy.GetChargeAsync(dbContext, stage.Kind, run.TenantId, coinCosts, cancellationToken);
            var canAfford = await AiPipelineCoinPolicy.CanAffordAsync(dbContext, charge, run.TenantId, run.TriggeredBy, role, cancellationToken);

            if (!canAfford)
            {
                stage.LastErrorKind = AiFailureKind.Coins;
                stage.LastError = await AiPipelineCoinPolicy.InsufficientMessageAsync(
                    dbContext, charge, stage.Kind, run.TenantId, run.TriggeredBy, role, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }
        }

        var inputs = await artifacts.GetCurrentPayloadsAsync(brand.Id, campaign?.Id, AllArtifactKinds, cancellationToken);
        var repairPrompt = AiPipelinePolicy.ShouldRepairPrompt(stage);
        var context = new StageContext(run, stage, brand, campaign, inputs, run.TriggeredBy, role, repairPrompt);

        stage.Status = AiPipelineStageStatus.Running;
        stage.StartedAt ??= DateTime.UtcNow;

        StageResult result;
        try
        {
            result = await _executorsByKind[stage.Kind].ExecuteAsync(context, cancellationToken);
        }
        catch (Exception ex)
        {
            result = StageResult.Failure(AiPipelinePolicy.ClassifyFailure(ex), ex.Message);
        }

        if (result.Succeeded)
        {
            await SettleSuccessAsync(run, stage, brand, campaign, role, result, cancellationToken);
        }
        else
        {
            SettleFailure(run, brand, campaign, stage, result);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SettleSuccessAsync(
        AiPipelineRun run, AiPipelineStage stage, TenantBrandProfile brand, MarketingCampaign? campaign,
        TenantMemberRole role, StageResult result, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        if (result.ReusedArtifactId is { } reusedId)
        {
            stage.ArtifactId = reusedId;
        }
        else if (result.ArtifactJson is not null)
        {
            // Sets stage.ArtifactId itself — the artifact and the stage that produced it are meant to
            // land in the same save, not be reconciled after the fact.
            await artifacts.AddVersionAsync(
                stage, run.TenantId, brand.Id, campaign?.Id, result.ArtifactKind!.Value,
                result.ArtifactJson, result.InputHash, cancellationToken);
        }

        if (result.FanOutTargets is { Count: > 0 } targets)
        {
            // Only ContentPlan fans out today; the child kind is the one thing FanOutTargets doesn't
            // carry, so it's resolved the same way TargetRefType conventions are elsewhere — by what
            // produced it, not by data on the result.
            var childDefinition = AiPipelinePolicy.Definition(AiPipelineStageKind.ContentImage);

            foreach (var targetId in targets)
            {
                dbContext.AiPipelineStages.Add(new AiPipelineStage
                {
                    Id = Guid.NewGuid(),
                    RunId = run.Id,
                    Kind = childDefinition.Kind,
                    Ordinal = childDefinition.Ordinal,
                    Status = AiPipelineStageStatus.Pending,
                    IsOptional = childDefinition.IsOptional,
                    MaxAttempts = childDefinition.MaxAttempts,
                    TargetRefId = targetId,
                    TargetRefType = "content_item",
                    CreatedAt = now
                });
            }
        }

        // A no-op for anything that isn't this stage's charge point, and for a stage already charged
        // (retry, re-run) — see AiPipelineCoinPolicy.TryChargeAsync.
        await AiPipelineCoinPolicy.TryChargeAsync(
            dbContext, stage, run, run.TriggeredBy, role, coinCosts, cancellationToken, result.ValueDelivered);

        stage.Status = AiPipelineStageStatus.Completed;
        stage.CompletedAt = now;
        stage.LastError = null;
        stage.LastErrorKind = null;
    }

    private void SettleFailure(
        AiPipelineRun run, TenantBrandProfile brand, MarketingCampaign? campaign, AiPipelineStage stage, StageResult result)
    {
        var now = DateTime.UtcNow;
        var failureKind = result.FailureKind ?? AiFailureKind.Unknown;
        var outcome = AiPipelinePolicy.ResolveFailure(stage, failureKind, now);

        stage.Status = outcome.Status;
        stage.NextAttemptAt = outcome.NextAttemptAt;
        stage.LastError = result.ErrorMessage;
        stage.LastErrorKind = failureKind;

        if (outcome.ConsumesAttempt)
        {
            stage.Attempts += 1;
        }

        // Per docs/AI_PIPELINE.md §6: a ContentItem must never end up with no image at all. This is
        // the one place that is true — every retry has just been exhausted, so nothing will produce a
        // real image for this post from here on. ContentImageExecutor deliberately doesn't do this
        // itself: it cannot tell "this attempt failed" from "every attempt is now exhausted", and
        // doing it on every failed attempt would leave a post with several placeholder rows behind it
        // by the time a real retry finally succeeds.
        if (outcome.Status == AiPipelineStageStatus.Skipped &&
            stage.Kind == AiPipelineStageKind.ContentImage &&
            stage.TargetRefId is { } contentItemId)
        {
            dbContext.VisualAssets.Add(new VisualAsset
            {
                Id = Guid.NewGuid(),
                ContentItemId = contentItemId,
                CampaignId = campaign?.Id,
                BrandProfileId = brand.Id,
                TenantId = run.TenantId,
                GenerationMode = GenerationMode.Campaign,
                Type = VisualAssetType.Image,
                SourceType = VisualAssetSourceType.Placeholder,
                FileUrl = "/text-post.png",
                IsApproved = false,
                CreatedAt = now
            });
        }
    }
}
