using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
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

        var brandProfileBelongsToTenant = await dbContext.TenantBrandProfiles
            .AnyAsync(b => b.Id == request.BrandProfileId && b.TenantId == tenantId, cancellationToken);
        if (!brandProfileBelongsToTenant)
        {
            return Result<CreateCampaignResponse>.Failure("Brand profile not found.");
        }

        var plan = await (
            from tenant in dbContext.Tenants
            join subscription in dbContext.Subscriptions on tenant.SubscriptionId equals subscription.Id
            join subscriptionPlan in dbContext.SubscriptionPlans on subscription.SubscriptionPlanId equals subscriptionPlan.Id
            where tenant.Id == tenantId
            select new { subscriptionPlan.Cost, subscriptionPlan.MaxCampaignsMonthly }
        ).FirstAsync(cancellationToken);

        if (plan.Cost <= 0)
        {
            return Result<CreateCampaignResponse>.Failure(
                "Creating campaigns requires an active paid subscription. Upgrade your plan to launch campaigns.");
        }

        var maxCampaignsMonthly = plan.MaxCampaignsMonthly;

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

        var campaign = new MarketingCampaign
        {
            Id = Guid.NewGuid(),
            BrandProfileId = request.BrandProfileId,
            CreatedBy = currentUserService.UserId!.Value,
            Name = request.Name,
            Objective = request.Objective,
            TargetPlatforms = targetPlatforms,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            BudgetAmount = request.BudgetAmount,
            BudgetCurrency = request.BudgetCurrency,
            Status = CampaignStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.MarketingCampaigns.Add(campaign);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<CreateCampaignResponse>.Success(
            new CreateCampaignResponse(campaign.Id, campaign.BrandProfileId, campaign.Name, campaign.Status));
    }
}
