import { CampaignPlatform } from './campaign.model';

export type AdStatus = 'active' | 'paused' | 'rejected' | 'pending' | 'completed';
export type AdFormat  = 'text' | 'image' | 'video' | 'carousel' | 'story' | 'reel';

export interface Ad {
  id: string;
  name: string;
  campaignId: string;
  campaignName: string;
  platforms: CampaignPlatform[];
  status: AdStatus;
  format: AdFormat;
  impressions: number;
  clicks: number;
  ctr: number;
  spend: number;
  cpc: number;
  createdAt: string;
  thumbnailColor?: string;
  /** Real image for format:'image' posts — ignored for 'text' (uses the
   *  static text-post.png) and 'video'/'carousel'/'story'/'reel' (uses a
   *  generic media placeholder, since we don't extract real video frames). */
  imageUrl?: string;
  /** Link to the live post — only set once a post has actually gone out,
   *  so the card's "open on platform" action can appear/disappear. */
  postUrl?: string;
}
