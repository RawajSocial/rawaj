namespace Rawaj.Application.Features.Campaigns.ScheduleCampaignPosts;

public record ScheduleCampaignPostResult(Guid ContentItemId, bool Succeeded, string? Error, Guid? ScheduledPostId, DateTime? ScheduledAt);

public record ScheduleCampaignPostsResponse(Guid CampaignId, int SucceededCount, int FailedCount, List<ScheduleCampaignPostResult> Results);
