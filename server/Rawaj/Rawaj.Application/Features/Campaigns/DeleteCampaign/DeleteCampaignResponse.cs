namespace Rawaj.Application.Features.Campaigns.DeleteCampaign;

/// <summary>
/// What the delete removed, as of the moment it committed. There is deliberately no "native cancel
/// failures" count any more: revoking platform-side scheduled posts now happens in the background
/// (see <see cref="CampaignCleanupPublisher"/>) with retries, so at the time this response is built
/// the outcome simply isn't known yet — reporting a number here could only ever be a guess. If the
/// cleanup ultimately gives up, the user is told through a notification instead.
/// </summary>
public record DeleteCampaignResponse(
    Guid CampaignId,
    int ContentItemsDeleted,
    int ImagesDeleted,
    int ScheduledPostsCancelled,
    int PublishedPostsLeftLive);
