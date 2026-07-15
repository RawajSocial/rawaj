import { CampaignStatus as ApiCampaignStatus, SocialPlatform } from '../core/models';

export type CampaignStatus = ApiCampaignStatus;
export type CampaignPlatform = SocialPlatform;

export interface Campaign {
  id: string;
  name: string;
  status: CampaignStatus;
  platforms: CampaignPlatform[];
  objective: string | null;
  budgetAmount: number | null;
  budgetCurrency: string | null;
  startDate: string | null;
  endDate: string | null;
  createdAt: string;
}
