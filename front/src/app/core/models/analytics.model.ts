import { SocialPlatform } from './enums';

export interface PostAnalyticsSnapshot {
  id: string;
  scheduledPostId: string;
  platform: SocialPlatform;
  recordedAt: string;
  impressions: number | null;
  reach: number | null;
  likes: number | null;
  comments: number | null;
  shares: number | null;
  saves: number | null;
  clicks: number | null;
  engagementRate: number | null;
}

export interface CampaignAnalyticsSummary {
  campaignId: string;
  postsTracked: number;
  totalImpressions: number;
  totalReach: number;
  totalLikes: number;
  totalComments: number;
  totalShares: number;
  averageEngagementRate: number | null;
  posts: PostAnalyticsSnapshot[];
}

export interface PlatformBreakdownItem {
  platform: SocialPlatform;
  reach: number;
  impressions: number;
}

export interface TopPostItem {
  scheduledPostId: string;
  contentItemId: string;
  platform: SocialPlatform;
  title: string | null;
  content: string;
  reach: number;
  likes: number;
  engagementRate: number | null;
}

export interface BrandAnalyticsOverview {
  brandProfileId: string;
  postsTracked: number;
  totalImpressions: number;
  totalReach: number;
  totalLikes: number;
  totalComments: number;
  totalShares: number;
  averageEngagementRate: number | null;
  platformBreakdown: PlatformBreakdownItem[];
  topPosts: TopPostItem[];
}
