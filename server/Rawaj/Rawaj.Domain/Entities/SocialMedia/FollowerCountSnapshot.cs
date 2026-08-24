using Rawaj.Domain.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Domain.Entities.SocialMedia;

/// <summary>
/// A point-in-time follower-count reading, appended on every successful FollowerCountSyncer run
/// alongside the in-place update to SocialAccount.FollowerCount - the account's field stays the
/// cheap "current value" read, while this table lets month-over-month (or any period) follower
/// growth actually be computed instead of only ever knowing the latest number.
/// </summary>
public class FollowerCountSnapshot : BaseEntity
{
    public Guid SocialAccountId { get; set; }
    public SocialPlatform Platform { get; set; }
    public DateTime RecordedAt { get; set; }
    public int FollowerCount { get; set; }

    public SocialAccount SocialAccount { get; set; } = null!;
}
