using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.CreateCampaign;

public class CreateCampaignCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext)
    : IRequestHandler<CreateCampaignCommand, Result<CreateCampaignResponse>>
{
    public async Task<Result<CreateCampaignResponse>> Handle(CreateCampaignCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var brand = await dbContext.TenantBrandProfiles
            .FirstOrDefaultAsync(b => b.Id == request.BrandProfileId && b.TenantId == tenantId, cancellationToken);
        if (brand is null)
        {
            return Result<CreateCampaignResponse>.Failure("Brand profile not found.");
        }

        // Every plan (including Free) now allows at least one campaign a month — campaign creation
        // is no longer gated behind a paid subscription.
        var tenantInfo = await (
            from tenant in dbContext.Tenants
            join subscription in dbContext.Subscriptions on tenant.SubscriptionId equals subscription.Id
            join subscriptionPlan in dbContext.SubscriptionPlans on subscription.SubscriptionPlanId equals subscriptionPlan.Id
            where tenant.Id == tenantId
            select new { subscriptionPlan.MaxCampaignsMonthly, tenant.IsActivated }
        ).FirstAsync(cancellationToken);

        // The frontend gates campaign pages behind tenant activation (brandAccessGuard) — this was
        // previously frontend-only; enforce it here too so it isn't just a UX gate an API client
        // could bypass. Only the tenant owner can complete activation (business-profile info).
        if (!tenantInfo.IsActivated)
        {
            return Result<CreateCampaignResponse>.Failure(
                "This tenant hasn't completed its business profile activation yet. The tenant owner must complete it in Settings first.");
        }

        var maxCampaignsMonthly = tenantInfo.MaxCampaignsMonthly;

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var campaignsThisMonth = await dbContext.MarketingCampaigns
            .Where(c => c.BrandProfile.TenantId == tenantId && c.CreatedAt >= monthStart)
            .CountAsync(cancellationToken);

        if (campaignsThisMonth >= maxCampaignsMonthly)
        {
            return Result<CreateCampaignResponse>.Failure(
                $"Your subscription plan allows a maximum of {maxCampaignsMonthly} campaign(s) per month. Upgrade to create more.");
        }

        var targetPlatforms = request.TargetPlatforms
            .Select(p => Enum.Parse<SocialPlatform>(p, true).ToString())
            .Distinct()
            .ToList();

        var userId = currentUserService.UserId!.Value;

        var campaign = new MarketingCampaign
        {
            Id = Guid.NewGuid(),
            BrandProfileId = request.BrandProfileId,
            CreatedBy = userId,
            Name = request.Name,
            Objective = request.Objective,
            TargetPlatforms = targetPlatforms,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            BudgetAmount = request.BudgetAmount,
            BudgetCurrency = request.BudgetCurrency,
            Status = CampaignStatus.Draft,
            BriefJson = request.BriefJson,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.MarketingCampaigns.Add(campaign);

        NotificationPublisher.Notify(
            dbContext, userId, brand.Id,
            NotificationType.Success, NotificationCategory.System,
            "Campaign created",
            $"\"{campaign.Name}\" was created for {brand.Name}.",
            campaign.Id, "marketing_campaign");

        AuditLogger.Log(
            dbContext, tenantId, userId, "campaign.created",
            message: $"Created campaign \"{campaign.Name}\" for {brand.Name}.",
            entityType: "marketing_campaign", entityId: campaign.Id, brandProfileId: brand.Id);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<CreateCampaignResponse>.Success(
            new CreateCampaignResponse(campaign.Id, campaign.BrandProfileId, campaign.Name, campaign.Status));
    }
}
