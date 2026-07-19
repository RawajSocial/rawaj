export type CampaignStatus   = 'active' | 'paused' | 'completed' | 'draft';
export type CampaignPlatform = 'instagram' | 'facebook' | 'tiktok' | 'youtube' | 'x' | 'snapchat' | 'linkedin';
export type CampaignObjective = 'awareness' | 'traffic' | 'engagement' | 'leads' | 'sales';

export interface Campaign {
  id: string;
  name: string;
  /** The brand profile this campaign belongs to — every campaign is scoped
   *  under a single brand profile umbrella (see BrandProfile). */
  brandProfileId: string;
  status: CampaignStatus;
  platforms: CampaignPlatform[];
  objective: CampaignObjective;
  industry?: string;
  budget: number;
  spent: number;
  reach: number;
  clicks: number;
  ctr: number;
  startDate: string;
  endDate: string;
  createdAt: string;
  adCount?: number;
  /** Per-campaign brand logo shown on the card banner. Falls back to the
   *  Rawaj logo (see CampaignCard) when not set. */
  logoUrl?: string;
}
