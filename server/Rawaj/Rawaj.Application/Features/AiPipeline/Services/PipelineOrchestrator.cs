using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Policies;
using Rawaj.Application.Features.AiPipeline.Common;
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
/// <para><b>One instance, one DbContext, sequential dispatch.</b> Multiple runs advancing at once —
/// what actually gives the pipeline concurrency — comes from the worker (C16) giving each run its own
/// scope and calling <see cref="AdvanceAsync"/> on each concurrently, not from parallelism inside a
/// single call here. What this class does provide for that world: an atomic per-stage claim (so two
/// callers racing the same <c>Pending</c> stage can't both dispatch it — a double-run is a double
/// provider charge, unlike the existing single-instance hosted services this pattern deliberately
/// diverges from), and a shared <see cref="IAiProviderConcurrencyLimiter"/> acquired around every
/// executor call, so a burst of runs all reaching a Groq stage in the same tick doesn't fire an
/// unbounded number of simultaneous requests at one provider.</para>
/// </summary>
public class PipelineOrchestrator(
    IApplicationDbContext dbContext,
    IPipelineArtifactStore artifacts,
    ICoinCostProvider coinCosts,
    IAiProviderConcurrencyLimiter providerLimiter,
    IEnumerable<IPipelineStageExecutor> executors,
    IServiceScopeFactory scopeFactory) : IPipelineOrchestrator
{
    private static readonly AiArtifactKind[] AllArtifactKinds = Enum.GetValues<AiArtifactKind>();

    private readonly Dictionary<AiPipelineStageKind, IPipelineStageExecutor> _executorsByKind =
        executors.ToDictionary(e => e.Kind);

    public async Task<AiPipelineRun> StartAsync(
        Guid tenantId, Guid brandProfileId, Guid? campaignId, Guid triggeredBy, CancellationToken cancellationToken,
        int? contentPostCount = null, Domain.Enums.Language? contentLanguage = null,
        bool? contentIncludeImages = null, Domain.Enums.ContentTemplateStyle? contentTemplateStyle = null)
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

        // Only overridden by callers that actually have a caller-specified value (today, the
        // generate-content legacy shim) — everyone else keeps AiPipelineRun's own defaults.
        if (contentPostCount is { } postCount) run.ContentPostCount = postCount;
        if (contentLanguage is { } language) run.ContentLanguage = language;
        if (contentIncludeImages is { } includeImages) run.ContentIncludeImages = includeImages;
        if (contentTemplateStyle is { } templateStyle) run.ContentTemplateStyle = templateStyle;
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

    public async Task AdvanceAsync(
        AiPipelineRun run, TenantMemberRole role, CancellationToken cancellationToken, string leaseOwner = "orchestrator")
    {
        while (true)
        {
            var stages = await LoadStagesAsync(run.Id, cancellationToken);
            var runnable = AiPipelinePolicy.GetRunnableStages(stages, DateTime.UtcNow);

            if (runnable.Count == 0)
            {
                break;
            }

            var dispatchable = new List<AiPipelineStage>();

            foreach (var stage in runnable)
            {
                if (AiPipelinePolicy.Definition(stage.Kind).RequiresHuman)
                {
                    // Never dispatched — the graph parks here until PipelineApprovalService clears it.
                    stage.Status = AiPipelineStageStatus.AwaitingApproval;
                    continue;
                }

                dispatchable.Add(stage);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            // Every stage in `dispatchable` was computed runnable from the same freshly-loaded
            // snapshot above, so none of them depends on another one in this same batch — dispatching
            // them all at once doesn't violate the graph. Each gets its own DB scope (see
            // ExecuteStageInIsolatedScopeAsync): EF Core's context cannot be shared across concurrent
            // operations, so genuine parallelism needs one context per in-flight stage, not one
            // shared across all of them the way a plain sequential loop could get away with.
            await Task.WhenAll(dispatchable.Select(stage =>
                ExecuteStageInIsolatedScopeAsync(run.Id, stage.Id, role, leaseOwner, cancellationToken)));

            // Every stage just dispatched ran in its own isolated scope, on its own database context
            // — but this outer context may already be tracking its own (now stale) copy of `run` and
            // of any of those same stage rows from an earlier query in this loop or an earlier
            // AdvanceAsync call on this run. EF Core's change tracker returns a tracked entity's own
            // in-memory values on a later query for that same row rather than the fresh values a
            // DIFFERENT context just wrote — without clearing it, `updatedStages` below would keep
            // reporting every dispatched stage's pre-dispatch status forever, EvaluateRunStatus would
            // never see anything as done, and this loop would never terminate (it would re-dispatch
            // the same "still pending" stage on every iteration). Clearing forces every query for the
            // rest of this iteration to read genuinely current values. IApplicationDbContext exposes
            // no ChangeTracker/Entry of its own (only DbSets) — every real implementation is an EF
            // Core DbContext, so this cast is the same escape hatch a mocked context in a unit test
            // would never need to go through.
            //
            // `run` is reattached below via its entry's State, not via DbSet.Attach(run): Attach()
            // cascades to every entity reachable from `run`'s object graph (Campaign, BrandProfile,
            // Stages), and EF's automatic relationship fixup silently populates those navigations with
            // whatever CLR instances this same context has ever tracked for this run — e.g. the
            // MarketingCampaign SeedAsync originally inserted, still sitting on `run.Campaign` with a
            // now-stale RowVersion, untouched by ChangeTracker.Clear() (that clears the *tracker*, not
            // plain CLR references other objects hold). Cascading through Attach() would try to
            // re-track those stale objects too — either colliding with genuinely fresh instances
            // something else in this iteration already loaded for the same key, or (for RowVersion'd
            // entities like MarketingCampaign) getting silently returned by a later tracked query for
            // that key instead of the fresh row, causing a spurious DbUpdateConcurrencyException on the
            // next save. Setting just this one entry's state touches nothing reachable from `run`.
            var efContext = (DbContext)dbContext;
            efContext.ChangeTracker.Clear();

            var updatedStages = await LoadStagesAsync(run.Id, cancellationToken);
            var newStatus = AiPipelinePolicy.EvaluateRunStatus(updatedStages);

            efContext.Entry(run).State = EntityState.Unchanged;
            await efContext.Entry(run).ReloadAsync(cancellationToken);

            run.Status = newStatus;
            run.UpdatedAt = DateTime.UtcNow;
            PipelineRunPublisher.Queue(dbContext, run, updatedStages);
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

    public async Task ExecuteSingleStageAsync(
        Guid runId, Guid stageId, TenantMemberRole role, CancellationToken cancellationToken, string leaseOwner)
    {
        var run = await dbContext.AiPipelineRuns.FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);
        var stage = await dbContext.AiPipelineStages.FirstOrDefaultAsync(s => s.Id == stageId, cancellationToken);

        // Both were real rows moments ago in the caller's own snapshot — gone here would mean the run
        // was deleted/cancelled out from under this stage between that snapshot and this scope
        // opening, which nothing in this codebase does today, but a no-op is the safe response either
        // way rather than throwing into a Task.WhenAll batch and failing every sibling with it.
        if (run is null || stage is null)
        {
            return;
        }

        await ExecuteStageAsync(run, stage, role, leaseOwner, cancellationToken);
    }

    /// <summary>Opens a brand new DI scope — and therefore a brand new <see cref="IApplicationDbContext"/>
    /// — to run one stage, so it can genuinely execute concurrently with siblings dispatched the same
    /// way. See the doc comment on <see cref="IPipelineOrchestrator.ExecuteSingleStageAsync"/> for why
    /// that isolation is required rather than optional.</summary>
    private async Task ExecuteStageInIsolatedScopeAsync(
        Guid runId, Guid stageId, TenantMemberRole role, string leaseOwner, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var scopedOrchestrator = scope.ServiceProvider.GetRequiredService<IPipelineOrchestrator>();
        await scopedOrchestrator.ExecuteSingleStageAsync(runId, stageId, role, cancellationToken, leaseOwner);
    }

    public async Task<AiPipelineStage> EnsureContentBatchAsync(AiPipelineRun run, CancellationToken cancellationToken)
    {
        var stage = await dbContext.AiPipelineStages
            .FirstOrDefaultAsync(s => s.RunId == run.Id && s.Kind == AiPipelineStageKind.ContentPlan, cancellationToken);

        if (stage is null)
        {
            var definition = AiPipelinePolicy.Definition(AiPipelineStageKind.ContentPlan);
            stage = new AiPipelineStage
            {
                Id = Guid.NewGuid(),
                RunId = run.Id,
                Kind = definition.Kind,
                Ordinal = definition.Ordinal,
                Status = AiPipelineStageStatus.Pending,
                IsOptional = definition.IsOptional,
                MaxAttempts = definition.MaxAttempts,
                CreatedAt = DateTime.UtcNow
            };
            dbContext.AiPipelineStages.Add(stage);
        }
        else if (stage.Status is AiPipelineStageStatus.Completed or AiPipelineStageStatus.Failed)
        {
            stage.Status = AiPipelineStageStatus.Pending;
            stage.NextAttemptAt = null;
            stage.CoinsCharged = 0;
        }

        // A run that already finished a previous batch sits at Completed — a terminal status the
        // background worker (AiPipelineWorkerHostedService) explicitly refuses to touch, regardless
        // of what any individual stage's own status is. Resetting the stage above without this would
        // leave that freshly-Pending ContentPlan row permanently stuck: candidate-selected by the
        // worker's loose stage-status query every 2 seconds, then rejected every time by its run.Status
        // gate. AdvanceAsync itself never checks run.Status before dispatching — only a caller sitting
        // in front of it (the worker) does — which is exactly why this went unnoticed until content
        // generation stopped calling AdvanceAsync directly and started depending on the worker alone.
        if (run.Status is AiPipelineRunStatus.Completed or AiPipelineRunStatus.Failed or AiPipelineRunStatus.Cancelled)
        {
            run.Status = AiPipelineRunStatus.Running;
            run.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return stage;
    }

    private Task<List<AiPipelineStage>> LoadStagesAsync(Guid runId, CancellationToken cancellationToken) =>
        dbContext.AiPipelineStages.Where(s => s.RunId == runId).ToListAsync(cancellationToken);

    private async Task ExecuteStageAsync(
        AiPipelineRun run, AiPipelineStage stage, TenantMemberRole role, string leaseOwner, CancellationToken cancellationToken)
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

        // The atomic claim. A single UPDATE ... WHERE Status = 'Pending', not a load-then-save — the
        // deliberate divergence from the existing hosted services, which document themselves as
        // single-instance-safe. AI stages cost real money, so two workers both dispatching the same
        // stage is a double provider charge, not merely duplicated work. Bypasses this DbContext's
        // change tracker, so the in-memory stage object is brought back in sync explicitly below
        // rather than reloaded — a second round trip for something we already know the result of.
        var leaseDuration = AiPipelinePolicy.Definition(stage.Kind).LeaseDuration;
        var leaseExpiresAt = DateTime.UtcNow.Add(leaseDuration);
        int claimed;

        try
        {
            claimed = await dbContext.AiPipelineStages
                .Where(s => s.Id == stage.Id && s.Status == AiPipelineStageStatus.Pending)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, AiPipelineStageStatus.Running)
                    .SetProperty(x => x.LeaseOwner, leaseOwner)
                    .SetProperty(x => x.LeaseExpiresAt, leaseExpiresAt)
                    .SetProperty(x => x.StartedAt, x => x.StartedAt ?? DateTime.UtcNow), cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // EF Core's InMemory provider — used by the test suite, never by a real deployment —
            // does not translate ExecuteUpdate at all and fails at translation time, before touching
            // any data, which is what makes this catch safe: a genuine SQL Server failure surfaces as
            // a DbUpdateException or a provider exception, never this one. InMemory also never runs
            // more than one writer at a time, so the atomicity this claim exists for is not something
            // a test could exercise through it anyway — a plain conditional write is equivalent there.
            claimed = stage.Status == AiPipelineStageStatus.Pending ? 1 : 0;
        }

        if (claimed == 0)
        {
            // Lost the race — another caller claimed this stage first. Nothing to undo: the coin
            // pre-flight check above spent nothing, it only read.
            return;
        }

        stage.Status = AiPipelineStageStatus.Running;
        stage.LeaseOwner = leaseOwner;
        stage.LeaseExpiresAt = leaseExpiresAt;
        stage.StartedAt ??= DateTime.UtcNow;

        var inputs = await artifacts.GetCurrentPayloadsAsync(brand.Id, campaign?.Id, AllArtifactKinds, cancellationToken);
        var repairPrompt = AiPipelinePolicy.ShouldRepairPrompt(stage);
        var context = new StageContext(run, stage, brand, campaign, inputs, run.TriggeredBy, role, repairPrompt);

        StageResult result;
        try
        {
            var provider = AiPipelinePolicy.Provider(stage.Kind);

            if (provider is null)
            {
                result = await _executorsByKind[stage.Kind].ExecuteAsync(context, cancellationToken);
            }
            else
            {
                using var lease = await providerLimiter.AcquireAsync(provider, cancellationToken);
                result = await _executorsByKind[stage.Kind].ExecuteAsync(context, cancellationToken);
            }
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

        // Pushed from right here, not just once for the whole dispatch wave in AdvanceAsync: with N
        // ContentImage stages fanned out and dispatched together, each one's own Cloudflare call
        // finishes on its own schedule, sometimes seconds apart, and this is the only point that
        // knows the moment any single one of them actually lands. Waiting for AdvanceAsync's
        // end-of-wave push instead would mean every post appears at once — after the *slowest* of
        // the batch, not each one as it's ready. This context only ever tracked this one stage
        // before now, so the query below reads every sibling fresh off the store, including ones
        // other isolated scopes have already completed concurrently.
        var currentStages = await dbContext.AiPipelineStages
            .Where(s => s.RunId == run.Id).ToListAsync(cancellationToken);
        PipelineRunPublisher.Queue(dbContext, run, currentStages);
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

        if (campaign is not null)
        {
            await WriteThroughLegacyColumnsAsync(run, campaign, brand, stage, result, now, cancellationToken);
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
        stage.LeaseOwner = null;
        stage.LeaseExpiresAt = null;
    }

    /// <summary>
    /// Phase 6 compatibility: keeps <c>CompetitorResearchJson</c>, <c>DiagnosisJson</c> and
    /// <c>AiPlanJson</c> current as projections of the artifacts that now produce them, so the
    /// existing strategy review UI, <c>campaign-strategy-page</c>, and every already-generated
    /// campaign keep reading a column that behaves exactly as it always has. Only three stages have a
    /// legacy column to project into — the rest (research queries, the three strategy sub-stages,
    /// content) never had one.
    /// </summary>
    private async Task WriteThroughLegacyColumnsAsync(
        AiPipelineRun run, MarketingCampaign campaign, TenantBrandProfile brand, AiPipelineStage stage,
        StageResult result, DateTime now, CancellationToken cancellationToken)
    {
        switch (stage.Kind)
        {
            case AiPipelineStageKind.CompetitorResearch when result.ArtifactJson is not null:
                campaign.CompetitorResearchJson = LegacyCampaignProjection.ProjectCompetitorResearch(result.ArtifactJson);
                campaign.UpdatedAt = now;
                break;

            case AiPipelineStageKind.CampaignAnalysis when result.ArtifactJson is not null:
                // DiagnosisJson is a union of the brand and campaign readings — CampaignAnalysis
                // always runs after BrandAnalysis (a graph dependency), so the brand's current
                // artifact is already saved by the time this stage settles.
                var brandAnalysis = await artifacts.GetCurrentAsync(
                    brand.Id, campaignId: null, AiArtifactKind.BrandAnalysis, cancellationToken);
                if (brandAnalysis is not null)
                {
                    campaign.DiagnosisJson = LegacyCampaignProjection.ProjectDiagnosis(
                        brandAnalysis.ContentJson, result.ArtifactJson);
                    campaign.UpdatedAt = now;
                }
                break;

            case AiPipelineStageKind.StrategyAssemble when result.ArtifactJson is not null:
                // Already built byte-compatible with AiPlanJson — see StrategyAssembleExecutor.
                campaign.AiPlanJson = result.ArtifactJson;
                campaign.AiGeneratedAt = now;
                campaign.UpdatedAt = now;

                // GenerateMarketingPlanCommandHandler always sent this — a real user-facing signal,
                // not an AI-mechanics detail, so it belongs here rather than in any one caller of the
                // orchestrator specifically.
                NotificationPublisher.Notify(
                    dbContext, run.TriggeredBy, brand.Id, NotificationType.Success, NotificationCategory.AiJob,
                    "Marketing plan ready", $"An AI-generated strategy for \"{campaign.Name}\" is ready to review.",
                    campaign.Id, "marketing_campaign");
                break;
        }
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
        stage.LeaseOwner = null;
        stage.LeaseExpiresAt = null;

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

        // ResearchCampaignCompetitorsCommandHandler always wrote *something* to
        // CompetitorResearchJson, even on failure — the "unavailable" sentinel, never null. A Skipped
        // CompetitorResearch stage never reaches SettleSuccessAsync's write-through, so without this
        // the legacy shim (C19) would see a null column where the old handler never left one.
        if (outcome.Status == AiPipelineStageStatus.Skipped &&
            stage.Kind == AiPipelineStageKind.CompetitorResearch &&
            campaign is not null)
        {
            campaign.CompetitorResearchJson = LegacyCampaignProjection.CompetitorResearchUnavailable(result.ErrorMessage);
            campaign.UpdatedAt = now;
        }
    }
}
