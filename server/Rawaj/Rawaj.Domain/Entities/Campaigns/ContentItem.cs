using Rawaj.Domain.Common;
using Rawaj.Domain.Entities.AiOperations;
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

    /// <summary>
    /// An <b>English</b> visual description of the image that should accompany this post, written by
    /// the text model at the same time as the post copy.
    /// <para>
    /// This exists because the image model (HuggingFace FLUX) is trained on English captions and
    /// produces poor results for Arabic input — and <see cref="Content"/>, which used to be sent
    /// straight to it, is Arabic marketing copy ("get 50% off now!"), which is not a description of
    /// a picture even once translated. Keeping the illustration prompt as its own field lets the
    /// post stay Arabic for the reader while the image model gets something it can actually render,
    /// and lets a later image retry reuse the same description instead of falling back to the copy.
    /// </para>
    /// Null for content items generated before this field existed, and for standalone (non-campaign)
    /// generations, which have no image-planning step — callers must fall back.
    /// </summary>
    public string? ImagePrompt { get; set; }
    /// <summary>The pipeline stage that produced this item, when it came from a campaign run. Null
    /// for standalone generations and for everything created before the pipeline existed. It is what
    /// makes the per-post image stages addressable: each ContentImage stage targets one item, so
    /// "which post is this retry for" is a lookup rather than an inference.</summary>
    public Guid? PipelineStageId { get; set; }

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
    public AiPipelineStage? PipelineStage { get; set; }
    public ICollection<ContentRevision> Revisions { get; set; } = [];
    public ICollection<VisualAsset> VisualAssets { get; set; } = [];
}
