import { CampaignPlatform } from './campaign.model';

export type AdStatus = 'active' | 'paused' | 'rejected' | 'pending' | 'completed';
export type AdFormat  = 'image' | 'video' | 'carousel' | 'story' | 'reel';

export interface Ad {
  id: string;
  name: string;
  campaignId: string;
  campaignName: string;
  platform: CampaignPlatform;
  status: AdStatus;
  format: AdFormat;
  impressions: number;
  clicks: number;
  ctr: number;
  spend: number;
  cpc: number;
  createdAt: string;
  thumbnailColor?: string;
}
