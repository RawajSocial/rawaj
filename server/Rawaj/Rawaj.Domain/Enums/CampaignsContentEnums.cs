namespace Rawaj.Domain.Enums;

public enum CampaignStatus
{
    Draft,
    Active,
    Paused,
    Completed,
    Archived
}

public enum ContentType
{
    Post,
    Story,
    ReelScript,
    AdCopy,
    Blog,
    Caption
}

public enum SocialPlatform
{
    Instagram,
    Linkedin,
    Twitter,
    Facebook,
    Tiktok,
    Youtube
}

public enum ContentStatus
{
    Draft,
    Reviewed,
    Approved,
    Rejected,
    Published
}

public enum VisualAssetType
{
    Image,
    Banner,
    Logo,
    Story,
    Ad,
    VideoThumbnail
}

public enum VisualAssetSourceType
{
    AiGenerated,
    UserUploaded,
    Edited,
    /// <summary>Stood in for a generation that couldn't actually produce an image (AI credits
    /// exhausted mid-batch, or the image model itself failed/ran out of quota) — every ContentItem
    /// still gets a VisualAsset, never a null image, and the user can retry for a real one later.</summary>
    Placeholder
}

public enum ContentTemplateStyle
{
    Auto,
    ProductHighlight,
    PromotionalOffer,
    EducationalTip,
    EngagementQuestion,
    BehindTheScenes,
    Testimonial,
    Announcement
}

/// <summary>
/// What a generated ContentItem/VisualAsset is scoped to — makes the business intent explicit in
/// the data rather than inferred from which of BrandProfileId/CampaignId happen to be null.
/// </summary>
public enum GenerationMode
{
    /// <summary>No brand profile — a "try the product" generation, not tied to any tenant brand.</summary>
    Standalone,
    /// <summary>A brand profile, but no specific campaign.</summary>
    Brand,
    /// <summary>Generated as part of a specific campaign (always implies a brand too).</summary>
    Campaign
}
