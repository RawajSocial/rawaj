namespace Rawaj.Application.Features.Admin.GetPlatformStats;

public record PlatformStatsResponse(
    int TotalTenants,
    int ActiveTenants,
    int TotalUsers,
    int ActiveSubscriptions,
    int TotalCampaigns,
    int TotalScheduledPosts,
    int TotalPublishedPosts,
    int TotalConnectedSocialAccounts);
