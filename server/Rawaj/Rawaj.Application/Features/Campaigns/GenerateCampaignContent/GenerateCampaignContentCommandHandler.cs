using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Application.Features.AiPipeline.Common;
using Rawaj.Application.Features.AiPipeline.Services;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.GenerateCampaignContent;

/// <summary>
/// C19's follow-up: a shim onto the pipeline orchestrator, request/response contract unchanged —
/// closes the one endpoint C19 itself deliberately left on its own handler. Unlike the other four
/// AI campaign actions, this one legitimately gets called more than once per campaign ("generate
/// another batch"), which the pipeline's single-<c>ContentPlan</c>-row-per-run model can't do by
/// re-running <see cref="IPipelineOrchestrator.AdvanceAsync"/> alone — <see cref="IPipelineOrchestrator.EnsureContentBatchAsync"/>
/// is what makes a second (or a backfilled campaign's first-ever) batch possible, by resetting that
/// one row rather than creating a second.
/// </summary>
public class GenerateCampaignContentCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext,
    IPipelineOrchestrator orchestrator)
    : IRequestHandler<GenerateCampaignContentCommand, Result<GenerateCampaignContentResponse>>
{
    public async Task<Result<GenerateCampaignContentResponse>> Handle(
        GenerateCampaignContentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var userId = currentUserService.UserId!.Value;
        var role = currentTenantContext.Role!.Value;

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

        var stage = await orchestrator.EnsureContentBatchAsync(run, cancellationToken);

        var existingItemIds = await dbContext.ContentItems
            .Where(c => c.CampaignId == campaign.Id)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        await orchestrator.AdvanceAsync(run, role, cancellationToken);

        var newItems = await dbContext.ContentItems
            .Where(c => c.CampaignId == campaign.Id && !existingItemIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        if (newItems.Count == 0)
        {
            var reloadedStage = await dbContext.AiPipelineStages.FirstAsync(s => s.Id == stage.Id, cancellationToken);
            var blockingError = reloadedStage.LastError
                ?? await LegacyPipelineGateway.BlockingErrorAsync(dbContext, run.Id, cancellationToken);
            return Result<GenerateCampaignContentResponse>.Failure(
                blockingError ?? "Campaign content generation failed. Please try again.");
        }

        var newItemIds = newItems.Select(i => i.Id).ToHashSet();
        var imagesGenerated = await dbContext.VisualAssets
            .CountAsync(v => v.ContentItemId != null && newItemIds.Contains(v.ContentItemId.Value)
                && v.SourceType == VisualAssetSourceType.AiGenerated, cancellationToken);

        NotificationPublisher.Notify(
            dbContext, userId, campaign.BrandProfileId, NotificationType.Info, NotificationCategory.ReviewNeeded,
            "Campaign drafts ready for review",
            $"{newItems.Count} new draft post(s) for \"{campaign.Name}\" are ready for your review.",
            campaign.Id, "marketing_campaign");

        await dbContext.SaveChangesAsync(cancellationToken);

        // AiCreditsPolicy's monthly image quota (and the mid-batch "skip for credits" it used to
        // cause) is retired in favour of coins as the single meter for this path — see C23. Every
        // ContentItem still never ends up with no image at all: ContentImageExecutor exhausting its
        // retries attaches the /text-post.png placeholder, same as before.
        return Result<GenerateCampaignContentResponse>.Success(
            new GenerateCampaignContentResponse(campaign.Id, newItems.Count, imagesGenerated));
    }
}
