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

        if (socialAccount.Platform != contentItem.Platform)
        {
            return Result<SchedulePostResponse>.Failure(
                $"This post is for {contentItem.Platform}, but the selected account is a {socialAccount.Platform} account.");
        }

        if (socialAccount.TokenExpiresAt is not null && socialAccount.TokenExpiresAt <= DateTime.UtcNow)
        {
            return Result<SchedulePostResponse>.Failure(
                $"The connection to this {socialAccount.Platform} account has expired. Reconnect it before scheduling.");
        }

        var alreadyScheduled = await dbContext.ScheduledPosts.AnyAsync(
            s => s.ContentItemId == contentItem.Id
                && (s.Status == ScheduledPostStatus.Pending || s.Status == ScheduledPostStatus.Published),
            cancellationToken);
        if (alreadyScheduled)
        {
            return Result<SchedulePostResponse>.Failure("This post is already scheduled or published.");
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
            return Result<SchedulePostResponse>.Failure(CoinPolicy.ScheduledPostCapMessage(maxScheduledPosts));
        }

        var coinCost = await CoinPricingPolicy.GetDiscountedCostAsync(dbContext, tenantId, coinCostProvider.Scheduling, cancellationToken);
        var coinBalance = await CoinPolicy.GetBalanceAsync(dbContext, tenantId, userId, role, cancellationToken);
        if (coinBalance < coinCost)
        {
            return Result<SchedulePostResponse>.Failure(
                CoinPolicy.InsufficientCoinsMessage(coinCost, coinBalance, "schedule this post"));
        }

        var now = DateTime.UtcNow;

        // PublishNow bypasses the "at least 10 minutes out" scheduling window entirely (see the
        // validator) - the row is still created with a real ScheduledAt (now) for record-keeping,
        // but ExecuteAsync is asked to publish immediately (scheduledAt: null) instead of handing
        // the time off to the platform's native scheduler.
        var scheduledPost = new ScheduledPost
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItem.Id,
            VisualAssetId = request.VisualAssetId,
            SocialAccountId = socialAccount.Id,
            BrandProfileId = contentItem.BrandProfileId ?? socialAccount.BrandProfileId,
            CampaignId = contentItem.CampaignId,
            ScheduledAt = request.PublishNow ? now : request.ScheduledAt,
            AiSuggestedTime = !request.PublishNow && request.AiSuggestedTime,
            Status = ScheduledPostStatus.Pending,
            RetryCount = 0,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.ScheduledPosts.Add(scheduledPost);
        await dbContext.SaveChangesAsync(cancellationToken);

        var handoffResult = request.PublishNow
            ? await ScheduledPostPublisher.PublishNowAsync(
                dbContext, tokenEncryptor, publishers, scheduledPost.Id, cancellationToken)
            : await ScheduledPostPublisher.ScheduleNativelyAsync(
                dbContext, tokenEncryptor, publishers, scheduledPost.Id, scheduledPost.ScheduledAt, cancellationToken);

        if (!handoffResult.Succeeded)
        {
            // Keep the row instead of deleting it - ExecuteAsync already marks it Failed with the
            // real reason in most cases, but a defensive set here covers every failure branch, so
            // the post stays visible (as Failed) on the calendar instead of vanishing.
            scheduledPost.Status = ScheduledPostStatus.Failed;
            scheduledPost.ErrorMessage = handoffResult.ErrorMessage ?? "Could not schedule with the platform.";
            scheduledPost.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result<SchedulePostResponse>.Failure(
                request.PublishNow
                    ? $"Could not publish to {socialAccount.Platform}: {scheduledPost.ErrorMessage}"
                    : $"Could not schedule with {socialAccount.Platform}: {scheduledPost.ErrorMessage}");
        }

        await CoinPolicy.TrySpendAsync(dbContext, tenantId, userId, role, coinCost, cancellationToken, reason: "post_scheduling");
        await dbContext.SaveChangesAsync(cancellationToken);

        var post = handoffResult.Data!;
        return Result<SchedulePostResponse>.Success(
            new SchedulePostResponse(post.Id, post.ScheduledAt, post.Status, post.PostId));
    }
}
