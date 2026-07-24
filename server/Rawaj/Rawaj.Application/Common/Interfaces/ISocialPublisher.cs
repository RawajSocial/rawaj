using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Interfaces;

public interface ISocialPublisher
{
    SocialPlatform Platform { get; }

    /// <summary>
    /// Whether this platform can accept a "publish later" request directly (e.g. Facebook's
    /// scheduled_publish_time). When false, ScheduledPostPublisher leaves the row Pending with no
    /// PostId instead of calling PublishAsync with a ScheduledAt, so the local background poller
    /// fires the actual publish when the time comes.
    /// </summary>
    bool SupportsNativeScheduling { get; }

    Task<PublishResult> PublishAsync(SocialPublishRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes a post already handed off to the platform's native scheduler (i.e. one where
    /// <see cref="SupportsNativeScheduling"/> is true and a <c>PostId</c> was already recorded).
    /// Must be called before cancelling or re-scheduling such a post locally, or the platform will
    /// publish it anyway regardless of what the local row says. Platforms without native
    /// scheduling never have anything to revoke, since nothing was ever handed off ahead of time.
    /// </summary>
    Task<PublishResult> CancelAsync(string accessToken, string externalPostId, CancellationToken cancellationToken);
}
