using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Interfaces;

public interface ISocialAnalyticsProvider
{
    SocialPlatform Platform { get; }

    Task<PostMetricsResult> GetMetricsAsync(string postId, string accessToken, CancellationToken cancellationToken);
}
