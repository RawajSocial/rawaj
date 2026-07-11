using Rawaj.Domain.Common;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Enums;

namespace Rawaj.Domain.Entities.SocialMedia;

public class ScheduledPost : BaseEntity
{
    public Guid ContentItemId { get; set; }
    public Guid? VisualAssetId { get; set; }
    public Guid SocialAccountId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public bool AiSuggestedTime { get; set; }
    public ScheduledPostStatus Status { get; set; }
    public string? PostId { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ContentItem ContentItem { get; set; } = null!;
    public VisualAsset? VisualAsset { get; set; }
    public SocialAccount SocialAccount { get; set; } = null!;
    public ICollection<PostAnalytics> Analytics { get; set; } = [];
}
