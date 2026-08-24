using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.AiPipeline.Services;

namespace Rawaj.Application.Features.Campaigns.GenerateCampaignContent;

/// <summary>
/// C19's follow-up: a shim onto the pipeline orchestrator. Unlike the other four AI campaign
/// actions, this one legitimately gets called more than once per campaign ("generate another
/// batch"), which the pipeline's single-<c>ContentPlan</c>-row-per-run model can't do by re-running
/// <see cref="IPipelineOrchestrator.AdvanceAsync"/> alone — <see cref="IPipelineOrchestrator.EnsureContentBatchAsync"/>
/// is what makes a second (or a backfilled campaign's first-ever) batch possible, by resetting that
/// one row rather than creating a second.
///
/// <para>Fire-and-track, matching <c>StartRunCommandHandler</c>: this only makes the batch's
/// <c>ContentPlan</c> stage runnable and returns — <see cref="Rawaj.Infrastructure.BackgroundJobs.AiPipelineWorkerHostedService"/>
/// (referenced here only in this comment; not linked, to avoid an Application→Infrastructure
/// reference) picks it up on its next poll and drives content and every image forward from there,
/// same as every other pipeline action. Previously this awaited <c>AdvanceAsync</c> directly and
/// blocked the request until the whole batch — text and every image — was done, which is also why
/// every post used to appear on the page at once: nothing was listening for progress mid-flight
/// because there was nothing to listen to yet.</para>
/// </summary>
public class GenerateCampaignContentCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentTenantContext currentTenantContext,
    IPipelineOrchestrator orchestrator)
    : IRequestHandler<GenerateCampaignContentCommand, Result<GenerateCampaignContentResponse>>
{
    public async Task<Result<GenerateCampaignContentResponse>> Handle(
        GenerateCampaignContentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var campaign = await dbContext.MarketingCampaigns
            .FirstOrDefaultAsync(c => c.Id == request.CampaignId && c.BrandProfile.TenantId == tenantId, cancellationToken);
        if (campaign is null)
        {
            return Result<GenerateCampaignContentResponse>.Failure("Campaign not found.");
        }

        if (campaign.PlanApprovedAt is null)
        {
            return Result<GenerateCampaignContentResponse>.Failure("Approve the campaign strategy before generating content.");
        }

        // Guaranteed by PipelineApprovalService and the C18 backfill migration alike — both always
        // set CurrentPipelineRunId in the same transaction as PlanApprovedAt.
        var run = await dbContext.AiPipelineRuns.FirstOrDefaultAsync(r => r.Id == campaign.CurrentPipelineRunId, cancellationToken);
        if (run is null)
        {
            return Result<GenerateCampaignContentResponse>.Failure("This campaign has no pipeline run to generate content from. Please try again.");
        }

        run.ContentPostCount = request.PostCount;
        run.ContentLanguage = request.Language;
        run.ContentIncludeImages = request.IncludeImages;
        run.ContentTemplateStyle = request.TemplateStyle;

        // Sets run.ContentPostCount etc (already assigned above, on the same tracked entity) and the
        // ContentPlan stage in the same SaveChangesAsync — nothing else for this handler to persist.
        await orchestrator.EnsureContentBatchAsync(run, cancellationToken);

        // AiCreditsPolicy's monthly image quota (and the mid-batch "skip for credits" it used to
        // cause) is retired in favour of coins as the single meter for this path — see C23. Every
        // ContentItem still never ends up with no image at all: ContentImageExecutor exhausting its
        // retries attaches the /text-post.png placeholder, same as before.
        return Result<GenerateCampaignContentResponse>.Success(
            new GenerateCampaignContentResponse(campaign.Id, run.Id, run.Status));
    }
}
