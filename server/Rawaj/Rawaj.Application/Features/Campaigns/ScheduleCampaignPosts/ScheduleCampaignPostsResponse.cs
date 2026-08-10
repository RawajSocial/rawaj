using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.ScheduleCampaignPosts;

public record ScheduleCampaignPostResult(
    Guid ContentItemId,
    SocialPlatform Platform,
    bool Succeeded,
    bool Skipped,
    string? Error,
    Guid? ScheduledPostId,
    DateTime? ScheduledAt,
    ScheduledPostStatus? Status = null);

public record ScheduleCampaignPostsResponse(
    Guid CampaignId,
    int SucceededCount,
    int FailedCount,
    int SkippedCount,
    List<SocialPlatform> MissingPlatforms,
    List<ScheduleCampaignPostResult> Results);
