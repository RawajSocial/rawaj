using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Application.Features.Scheduling.Common;
using Rawaj.Domain.Entities.SocialMedia;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Scheduling.SchedulePost;

public class SchedulePostCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext,
    ITokenEncryptor tokenEncryptor,
    IEnumerable<ISocialPublisher> publishers,
    ICoinCostProvider coinCostProvider)
    : IRequestHandler<SchedulePostCommand, Result<SchedulePostResponse>>
{
    public async Task<Result<SchedulePostResponse>> Handle(SchedulePostCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var userId = currentUserService.UserId!.Value;
        var role = currentTenantContext.Role!.Value;

        var contentItem = await dbContext.ContentItems
            .FirstOrDefaultAsync(c => c.Id == request.ContentItemId && c.TenantId == tenantId, cancellationToken);
        if (contentItem is null)
        {
            return Result<SchedulePostResponse>.Failure("Content item not found.");
        }

        if (contentItem.Status != ContentStatus.Approved)
        {
            return Result<SchedulePostResponse>.Failure("Only approved content can be scheduled.");
        }

        var socialAccount = await dbContext.SocialAccounts.FirstOrDefaultAsync(
            s => s.Id == request.SocialAccountId
                && s.BrandProfileId == contentItem.BrandProfileId
                && s.IsActive,
            cancellationToken);
        if (socialAccount is null)
        {
            return Result<SchedulePostResponse>.Failure("Social account not found or not connected to this brand.");
        }

        if (request.VisualAssetId is not null)
        {
            var visualAssetValid = await dbContext.VisualAssets.AnyAsync(
                v => v.Id == request.VisualAssetId && v.BrandProfileId == contentItem.BrandProfileId,
                cancellationToken);
            if (!visualAssetValid)
            {
                return Result<SchedulePostResponse>.Failure("Visual asset not found.");
            }
        }

        var maxScheduledPosts = await (
            from tenant in dbContext.Tenants
            join subscription in dbContext.Subscriptions on tenant.SubscriptionId equals subscription.Id
            join plan in dbContext.SubscriptionPlans on subscription.SubscriptionPlanId equals plan.Id
            where tenant.Id == tenantId
            select plan.MaxScheduledPosts
        ).FirstAsync(cancellationToken);

        var activeScheduledCount = await dbContext.ScheduledPosts
            .Where(s => s.ContentItem.TenantId == tenantId
                && (s.Status == ScheduledPostStatus.Pending || s.Status == ScheduledPostStatus.Published))
            .CountAsync(cancellationToken);

        if (activeScheduledCount >= maxScheduledPosts)
        {
            return Result<SchedulePostResponse>.Failure(
                $"Your subscription plan allows a maximum of {maxScheduledPosts} scheduled post(s). Upgrade for more.");
        }

        var coinCost = await CoinPricingPolicy.GetDiscountedCostAsync(dbContext, tenantId, coinCostProvider.Scheduling, cancellationToken);
        var coinBalance = await CoinPolicy.GetBalanceAsync(dbContext, tenantId, userId, role, cancellationToken);
        if (coinBalance < coinCost)
        {
            return Result<SchedulePostResponse>.Failure(
                $"You need {coinCost} coins to schedule this post, but only have {coinBalance}.");
        }

        var now = DateTime.UtcNow;

        var scheduledPost = new ScheduledPost
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItem.Id,
            VisualAssetId = request.VisualAssetId,
            SocialAccountId = socialAccount.Id,
            BrandProfileId = contentItem.BrandProfileId ?? socialAccount.BrandProfileId,
            CampaignId = contentItem.CampaignId,
            ScheduledAt = request.ScheduledAt,
            AiSuggestedTime = request.AiSuggestedTime,
            Status = ScheduledPostStatus.Pending,
            RetryCount = 0,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.ScheduledPosts.Add(scheduledPost);
        await dbContext.SaveChangesAsync(cancellationToken);

        var handoffResult = await ScheduledPostPublisher.ScheduleNativelyAsync(
            dbContext, tokenEncryptor, publishers, scheduledPost.Id, scheduledPost.ScheduledAt, cancellationToken);

        if (!handoffResult.Succeeded)
        {
            // The platform rejected or could not accept the schedule request; don't leave a
            // phantom local-only row behind.
            dbContext.ScheduledPosts.Remove(scheduledPost);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result<SchedulePostResponse>.Failure(
                $"Could not schedule with {socialAccount.Platform}: {handoffResult.ErrorMessage}");
        }

        await CoinPolicy.TrySpendAsync(dbContext, tenantId, userId, role, coinCost, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var post = handoffResult.Data!;
        return Result<SchedulePostResponse>.Success(
            new SchedulePostResponse(post.Id, post.ScheduledAt, post.Status, post.PostId));
    }
}
