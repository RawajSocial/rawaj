using Rawaj.Domain.Common;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Domain.Entities.Campaigns;

public class VisualAsset : BaseEntity
{
    public Guid? ContentItemId { get; set; }
    public Guid? CampaignId { get; set; }
    public Guid? BrandProfileId { get; set; }
    /// <summary>Mirrors ContentItem.TenantId — needed for tenant-scoped lookups (e.g. ReviewVisualAsset)
    /// to still work for Standalone assets, which have no BrandProfile to reach TenantId through.</summary>
    public Guid? TenantId { get; set; }
    public GenerationMode GenerationMode { get; set; }
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
    public DateTime CreatedAt { get; set; }

    /// <summary>The storage provider's id for this asset (e.g. Cloudinary public_id) — required to
    /// later delete or replace it. Null for pre-Cloudinary rows that still hold a base64 data URI.</summary>
    public string? PublicId { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? MimeType { get; set; }
    /// <summary>"Cloudinary" today; ready for future providers (S3/Azure Blob) without a schema change.</summary>
    public string? StorageProvider { get; set; }
    public DateTime? UploadedAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public ContentItem? ContentItem { get; set; }
    public MarketingCampaign? Campaign { get; set; }
    public TenantBrandProfile? BrandProfile { get; set; }
    public VisualAsset? VersionOfAsset { get; set; }
}
