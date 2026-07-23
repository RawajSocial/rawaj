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
