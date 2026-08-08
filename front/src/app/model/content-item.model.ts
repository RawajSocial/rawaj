/** Mirrors the backend's `SocialPlatform` enum (serialized as a PascalCase string). */
export type BackendSocialPlatform = 'Instagram' | 'Linkedin' | 'Twitter' | 'Facebook' | 'Tiktok' | 'Youtube';

/** GET /api/v1/content-items — Rawaj.Application.Features.Content.GetContentItems.ContentItemSummary */
export interface ContentItemSummary {
  contentItemId: string;
  contentType: 'Post' | 'Story' | 'ReelScript' | 'AdCopy' | 'Blog' | 'Caption';
  platform: BackendSocialPlatform;
  language: 'En' | 'Ar';
  content: string;
  status: 'Draft' | 'Reviewed' | 'Approved' | 'Rejected' | 'Published';
  createdAt: string;
  suggestedPostAt?: string | null;
  visualAssetId?: string | null;
  imageUrl?: string | null;
}

/** POST /api/v1/content-items/{id}/review — ReviewContentItemResponse */
export interface ReviewContentItemResponse {
  contentItemId: string;
  status: ContentItemSummary['status'];
  reviewedAt: string;
}

/** POST /api/v1/content-items/{id}/regenerate — RegenerateContentItemResponse */
export interface RegenerateContentItemResponse {
  contentItemId: string;
  content: string;
  status: ContentItemSummary['status'];
  revisionNumber: number;
}

/** POST /api/v1/campaigns/{id}/generate-content request body. */
export interface GenerateCampaignContentInput {
  postCount: number;
  language: 'En' | 'Ar';
  includeImages?: boolean;
  templateStyle?: 'Auto' | string;
}

/** POST /api/v1/campaigns/{id}/generate-content — GenerateCampaignContentResponse */
export interface GenerateCampaignContentResponse {
  campaignId: string;
  generatedCount: number;
  imagesGenerated: number;
}

/** POST /api/v1/content-items/generate request body — GenerateContentItemCommand.
 *  `brandProfileId` is optional — omitting it produces a "standalone" generation not tied to any
 *  brand, for trying the product out before a brand profile exists. */
export interface GenerateContentItemInput {
  brandProfileId?: string;
  campaignId?: string;
  contentType: ContentItemSummary['contentType'];
  platform: BackendSocialPlatform;
  language: 'En' | 'Ar';
  tone?: string;
  additionalInstructions?: string;
  templateStyle?: 'Auto' | string;
}

/** POST /api/v1/content-items/generate — GenerateContentItemResponse */
export interface GenerateContentItemResponse {
  contentItemId: string;
  campaignId?: string | null;
  content: string;
  status: ContentItemSummary['status'];
}
