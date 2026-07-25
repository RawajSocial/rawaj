using Rawaj.Domain.Common;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Domain.Entities.Campaigns;

public class ContentItem : BaseEntity, IConcurrencyAware
{
    public Guid? CampaignId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? BrandProfileId { get; set; }
    public GenerationMode GenerationMode { get; set; }
    public Guid CreatedBy { get; set; }
    public ContentType ContentType { get; set; }
    public SocialPlatform Platform { get; set; }
    public Language Language { get; set; }
    public string? Title { get; set; }
    public string Content { get; set; } = null!;
    public List<string> Hashtags { get; set; } = [];
    public string? Cta { get; set; }
    public string? Tone { get; set; }
    public string? AiPromptUsed { get; set; }
    public ContentStatus Status { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? SuggestedPostAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public MarketingCampaign? Campaign { get; set; }
    public TenantBrandProfile? BrandProfile { get; set; }
    public ICollection<ContentRevision> Revisions { get; set; } = [];
    public ICollection<VisualAsset> VisualAssets { get; set; } = [];
}
