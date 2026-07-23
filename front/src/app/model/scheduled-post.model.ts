import { CampaignPlatform } from './campaign.model';
import { BackendSocialPlatform } from './content-item.model';

export type PostStatus = 'scheduled' | 'published' | 'failed' | 'draft';
export type MediaType  = 'image' | 'video' | 'carousel' | 'reel' | 'story';

export interface ScheduledPost {
  id: string;
  campaignId: string;
  campaignName: string;
  platform: CampaignPlatform;
  content: string;
  mediaType?: MediaType;
  scheduledAt: string; // ISO: "2026-06-04T10:30:00"
  status: PostStatus;
  hashtags?: string[];
  estimatedReach?: number;
}

/** GET /api/v1/scheduled-posts — Rawaj.Application.Features.Scheduling.GetScheduledPosts.ScheduledPostSummary */
export interface ScheduledPostSummary {
  scheduledPostId: string;
  contentItemId: string;
  brandProfileId: string;
  campaignId?: string | null;
  platform: BackendSocialPlatform;
  accountName: string;
  scheduledAt: string;
  status: 'Pending' | 'Published' | 'Failed' | 'Cancelled';
  publishedAt?: string | null;
  errorMessage?: string | null;
  impressions?: number | null;
  reach?: number | null;
  likes?: number | null;
  comments?: number | null;
  shares?: number | null;
  clicks?: number | null;
  engagementRate?: number | null;
}
