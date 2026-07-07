import { CampaignPlatform } from './campaign.model';

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
