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
}
