using Rawaj.Domain.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Domain.Entities.SocialMedia;

public class PostAnalytics : BaseEntity
{
    public Guid ScheduledPostId { get; set; }
    public SocialPlatform Platform { get; set; }
    public DateTime RecordedAt { get; set; }
    public long? Impressions { get; set; }
    public long? Reach { get; set; }
    public int? Likes { get; set; }
    public int? Comments { get; set; }
    public int? Shares { get; set; }
    public int? Saves { get; set; }
    public int? Clicks { get; set; }
    public decimal? EngagementRate { get; set; }
    public string? RawData { get; set; }

    public ScheduledPost ScheduledPost { get; set; } = null!;
}
