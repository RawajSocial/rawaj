using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Application.Features.AiPipeline.Services;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.ApproveCampaignPlan;

/// <summary>
/// Stamps PlanApprovedAt once the user signs off on the generated strategy — this is the gate
/// GenerateCampaignContentCommandHandler checks before it will produce any posts, so nothing gets
/// generated against a plan nobody has actually reviewed and approved.
///
/// <para>C19 follow-up: when the campaign has a pipeline run parked at <c>HumanApproval</c> — true
/// for every strategy generated through C19's shims, and for any run started directly through
/// <c>StartRunCommand</c> — this delegates to <see cref="IPipelineApprovalService"/> so the run's own
/// state moves in step with the legacy column, rather than leaving that stage stuck
/// <c>AwaitingApproval</c> forever while <c>PlanApprovedAt</c> says otherwise. Falls back to setting
/// the column directly for anything that predates a real run reaching that stage (a C18-backfilled
/// campaign already approved before this existed, or any other record with no matching run) —
/// approval must not start failing for campaigns this code has never seen.</para>
/// </summary>
public class ApproveCampaignPlanCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext,
    IPipelineApprovalService approvalService)
    : IRequestHandler<ApproveCampaignPlanCommand, Result<ApproveCampaignPlanResponse>>
{
    public async Task<Result<ApproveCampaignPlanResponse>> Handle(
        ApproveCampaignPlanCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var userId = currentUserService.UserId!.Value;

        var campaign = await dbContext.MarketingCampaigns
            .FirstOrDefaultAsync(c => c.Id == request.CampaignId && c.BrandProfile.TenantId == tenantId, cancellationToken);
        if (campaign is null)
        {
            return Result<ApproveCampaignPlanResponse>.Failure("Campaign not found.");
        }

        if (string.IsNullOrWhiteSpace(campaign.AiPlanJson))
        {
            return Result<ApproveCampaignPlanResponse>.Failure("Generate a strategy before approving it.");
        }

        var run = campaign.CurrentPipelineRunId is { } runId
            ? await dbContext.AiPipelineRuns.FirstOrDefaultAsync(r => r.Id == runId, cancellationToken)
            : null;
        var awaitingApproval = run is not null && await dbContext.AiPipelineStages.AnyAsync(
            s => s.RunId == run.Id && s.Kind == AiPipelineStageKind.HumanApproval
                && s.Status == AiPipelineStageStatus.AwaitingApproval,
            cancellationToken);

        if (run is not null && awaitingApproval)
        {
            var approved = await approvalService.ApproveAsync(run, campaign, cancellationToken);
            if (!approved.Succeeded)
            {
                return Result<ApproveCampaignPlanResponse>.Failure(approved.ErrorMessage!);
            }
        }
        else
        {
            var now = DateTime.UtcNow;
            campaign.PlanApprovedAt = now;
            if (campaign.Status == CampaignStatus.Draft)
            {
                campaign.Status = CampaignStatus.Active;
            }
            campaign.UpdatedAt = now;
        }

        NotificationPublisher.Notify(
            dbContext, userId, campaign.BrandProfileId,
            NotificationType.Success, NotificationCategory.System,
            "Campaign strategy approved",
            $"The strategy for \"{campaign.Name}\" was approved. You can now generate content for it.",
            campaign.Id, "marketing_campaign");

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<ApproveCampaignPlanResponse>.Success(
            new ApproveCampaignPlanResponse(campaign.Id, campaign.Status, campaign.PlanApprovedAt!.Value));
    }
}
