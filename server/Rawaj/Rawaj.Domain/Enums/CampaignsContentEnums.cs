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
    Edited
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
