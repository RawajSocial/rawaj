using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Interfaces;

/// <summary>
/// Confirms whether a post handed off to a platform's own native scheduler has actually gone
/// live yet, since a successful handoff only means the platform accepted the request - it does
/// not guarantee the post published (the platform can silently drop it, e.g. Page unpublished,
/// content policy violation).
/// </summary>
public interface ISocialPostStatusChecker
{
    SocialPlatform Platform { get; }

    Task<SocialPostStatusResult> CheckStatusAsync(
        string accountIdExternal, string postId, string accessToken, CancellationToken cancellationToken);
}
