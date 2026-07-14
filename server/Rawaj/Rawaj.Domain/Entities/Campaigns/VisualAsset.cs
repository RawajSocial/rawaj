using Rawaj.Domain.Common;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Domain.Entities.Campaigns;

public class VisualAsset : BaseEntity
{
    public Guid? ContentItemId { get; set; }
    public Guid? CampaignId { get; set; }
    public Guid BrandProfileId { get; set; }
    public VisualAssetType Type { get; set; }
    public string FileUrl { get; set; } = null!;
    public VisualAssetSourceType SourceType { get; set; }
    public string? AiPrompt { get; set; }
    public string? AiModel { get; set; }
    public int? WidthPx { get; set; }
    public int? HeightPx { get; set; }
    public string? Format { get; set; }
    public bool IsApproved { get; set; }
    public Guid? VersionOf { get; set; }

    public ContentItem? ContentItem { get; set; }
    public MarketingCampaign? Campaign { get; set; }
    public TenantBrandProfile BrandProfile { get; set; } = null!;
    public VisualAsset? VersionOfAsset { get; set; }
}
