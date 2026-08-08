using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Application.Features.Campaigns.GetCampaign;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.UpdateCampaign;

public class UpdateCampaignCommandHandler(
    IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext, ICurrentUserService currentUserService)
    : IRequestHandler<UpdateCampaignCommand, Result<GetCampaignResponse>>
{
    public async Task<Result<GetCampaignResponse>> Handle(UpdateCampaignCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var campaign = await dbContext.MarketingCampaigns
            .FirstOrDefaultAsync(c => c.Id == request.CampaignId && c.BrandProfile.TenantId == tenantId, cancellationToken);
        if (campaign is null)
        {
            return Result<GetCampaignResponse>.Failure("Campaign not found.");
        }

        // UpdateCampaignCommandValidator can only compare the two dates when a request carries
        // both. A patch that moves just one of them (the campaign edit form sends only what
        // changed) has to be checked against the value already stored, or an end date could be
        // saved before its own start date.
        var resolvedStart = request.StartDate ?? campaign.StartDate;
        var resolvedEnd = request.EndDate ?? campaign.EndDate;
        if (resolvedStart is { } start && resolvedEnd is { } end && end < start)
        {
            return Result<GetCampaignResponse>.Failure("End date must be on or after the start date.");
        }

        // This same command is also called on every autosave tick while the onboarding wizard is
        // running (name/dates/budget/briefJson patched silently, many times a minute) — only a
        // real status transition (pause/resume, an explicit user action) is worth a notification;
        // notifying on every autosave field patch would spam the feed during onboarding.
        var previousStatus = campaign.Status;

        if (request.Name is not null) campaign.Name = request.Name;
        if (request.Status is not null) campaign.Status = request.Status.Value;
        if (request.StartDate is not null) campaign.StartDate = request.StartDate;
        if (request.EndDate is not null) campaign.EndDate = request.EndDate;
        if (request.BudgetAmount is not null) campaign.BudgetAmount = request.BudgetAmount;
        if (request.Objective is not null) campaign.Objective = request.Objective;
        if (request.BudgetCurrency is not null) campaign.BudgetCurrency = request.BudgetCurrency;
        if (request.BriefJson is not null) campaign.BriefJson = request.BriefJson;
        if (request.MarkOnboardingCompleted && campaign.OnboardingCompletedAt is null)
        {
            campaign.OnboardingCompletedAt = DateTime.UtcNow;
        }
        if (request.TargetPlatforms is not null)
        {
            campaign.TargetPlatforms = request.TargetPlatforms
                .Select(p => Enum.Parse<SocialPlatform>(p, true).ToString())
                .Distinct()
                .ToList();
        }
        campaign.UpdatedAt = DateTime.UtcNow;

        if (request.Status is not null && request.Status.Value != previousStatus)
        {
            var userId = currentUserService.UserId!.Value;
            var statusLabel = request.Status.Value switch
            {
                CampaignStatus.Active => "resumed",
                CampaignStatus.Paused => "paused",
                CampaignStatus.Archived => "archived",
                CampaignStatus.Completed => "marked completed",
                _ => "updated",
            };

            NotificationPublisher.Notify(
                dbContext, userId, campaign.BrandProfileId,
                NotificationType.Info, NotificationCategory.System,
                "Campaign status changed",
                $"\"{campaign.Name}\" was {statusLabel}.",
                campaign.Id, "marketing_campaign");

            AuditLogger.Log(
                dbContext, tenantId, userId, "campaign.status_changed",
                message: $"\"{campaign.Name}\" was {statusLabel}.",
                entityType: "marketing_campaign", entityId: campaign.Id, brandProfileId: campaign.BrandProfileId);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new GetCampaignResponse(
            campaign.Id,
            campaign.BrandProfileId,
            campaign.Name,
            campaign.Objective,
            campaign.TargetPlatforms,
            campaign.StartDate,
            campaign.EndDate,
            campaign.BudgetAmount,
            campaign.BudgetCurrency,
            campaign.Status,
            campaign.AiPlanJson,
            campaign.AiGeneratedAt,
            campaign.BriefJson,
            campaign.CompetitorResearchJson,
            campaign.DiagnosisJson,
            campaign.PlanApprovedAt,
            campaign.CreatedAt,
            campaign.UpdatedAt,
            campaign.CurrentPipelineRunId,
            campaign.OnboardingCompletedAt);

        return Result<GetCampaignResponse>.Success(response);
    }
}
