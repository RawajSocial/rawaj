// String literal unions mirror the backend's JsonStringEnumConverter output exactly
// (PascalCase, matching the C# enum member names in Rawaj.Domain.Enums) so no mapping
// layer is needed between the wire format and the UI.

export type Language = 'En' | 'Ar';
export type TenantType = 'Business' | 'Agency' | 'Freelancer';
export type BrandVoice = 'Professional' | 'Playful' | 'Bold' | 'Friendly' | 'Formal';
export type BrandProfileStatus = 'Active' | 'Archived' | 'Draft';
export type TenantMemberRole = 'Owner' | 'Admin' | 'Editor' | 'Viewer';
export type InvitationStatus = 'Pending' | 'Accepted' | 'Declined';

export type CampaignStatus = 'Draft' | 'Active' | 'Paused' | 'Completed' | 'Archived';
export type ContentType = 'Post' | 'Story' | 'ReelScript' | 'AdCopy' | 'Blog' | 'Caption';
export type SocialPlatform = 'Instagram' | 'Linkedin' | 'Twitter' | 'Facebook' | 'Tiktok' | 'Youtube';
export type ContentStatus = 'Draft' | 'Reviewed' | 'Approved' | 'Rejected' | 'Published';
export type VisualAssetType = 'Image' | 'Banner' | 'Logo' | 'Story' | 'Ad' | 'VideoThumbnail';

export type CompetitorStatus = 'Active' | 'Archived';

export type ScheduledPostStatus = 'Pending' | 'Published' | 'Failed' | 'Cancelled';

export type BillingCycle = 'Monthly' | 'Yearly';
export type SubscriptionStatus = 'Active' | 'Cancelled' | 'PastDue' | 'Trialing';

export type NotificationType = 'Info' | 'Success' | 'Warning' | 'Error';
export type NotificationCategory = 'AiJob' | 'PostPublished' | 'PostFailed' | 'ReviewNeeded' | 'Billing' | 'System';
