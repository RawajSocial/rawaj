using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Admin.GetPlatformStats;

public class GetPlatformStatsQueryHandler(IApplicationDbContext dbContext, IIdentityService identityService)
    : IRequestHandler<GetPlatformStatsQuery, Result<PlatformStatsResponse>>
{
    public async Task<Result<PlatformStatsResponse>> Handle(GetPlatformStatsQuery request, CancellationToken cancellationToken)
    {
        var totalTenants = await dbContext.Tenants.CountAsync(cancellationToken);
        var activeTenants = await dbContext.Tenants.CountAsync(t => t.IsActive, cancellationToken);
        var activeSubscriptions = await dbContext.Subscriptions.CountAsync(s => s.Status == SubscriptionStatus.Active, cancellationToken);
        var totalCampaigns = await dbContext.MarketingCampaigns.CountAsync(cancellationToken);
        var totalScheduledPosts = await dbContext.ScheduledPosts.CountAsync(cancellationToken);
        var totalPublishedPosts = await dbContext.ScheduledPosts.CountAsync(s => s.Status == ScheduledPostStatus.Published, cancellationToken);
        var totalConnectedSocialAccounts = await dbContext.SocialAccounts.CountAsync(s => s.IsActive, cancellationToken);

        var (_, totalUsers) = await identityService.ListUsersAsync(1, 1, cancellationToken);

        return Result<PlatformStatsResponse>.Success(new PlatformStatsResponse(
            totalTenants,
            activeTenants,
            totalUsers,
            activeSubscriptions,
            totalCampaigns,
            totalScheduledPosts,
            totalPublishedPosts,
            totalConnectedSocialAccounts));
    }
}
