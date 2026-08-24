using Rawaj.Domain.Common;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Domain.Entities.SocialMedia;

public class SocialAccount : BaseEntity
{
    public Guid BrandProfileId { get; set; }
    public string? Type { get; set; }
    public SocialPlatform Platform { get; set; }
    public string AccountName { get; set; } = null!;
    public string AccountIdExternal { get; set; } = null!;
    public string Token { get; set; } = null!;
    public string? RefreshTokenEnc { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public List<string> Scopes { get; set; } = [];
    public bool IsActive { get; set; }
    public DateTime? LastVerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Latest known follower count from the platform (Facebook Page's <c>followers_count</c>
    /// or Instagram Business account's <c>followers_count</c>), refreshed periodically by
    /// <see cref="Rawaj.Application.Common.Interfaces.ISocialFollowerCountProvider"/>. Null until the
    /// first successful sync.</summary>
    public int? FollowerCount { get; set; }
    public DateTime? FollowerCountSyncedAt { get; set; }

    public TenantBrandProfile BrandProfile { get; set; } = null!;
    public ICollection<ScheduledPost> ScheduledPosts { get; set; } = [];
}
