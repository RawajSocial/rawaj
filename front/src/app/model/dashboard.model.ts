import { BackendSocialPlatform } from './content-item.model';

/** Rawaj.Application.Features.Analytics shared type, reused by DashboardOverviewResponse. */
export interface PlatformBreakdownItem {
  platform: BackendSocialPlatform;
  reach: number;
  impressions: number;
}

/** Rawaj.Application.Features.Analytics shared type, reused by DashboardOverviewResponse. */
export interface TopPostItem {
  scheduledPostId: string;
  contentItemId: string;
  campaignId?: string | null;
  platform: BackendSocialPlatform;
  title?: string | null;
  content: string;
  reach: number;
  likes: number;
  engagementRate?: number | null;
}

/** GET /api/v1/dashboard/overview */
export interface DashboardOverviewResponse {
  brandProfileId: string;
  campaignId?: string | null;
  postsTracked: number;
  totalImpressions: number;
  totalReach: number;
  totalLikes: number;
  totalComments: number;
  totalShares: number;
  averageEngagementRate?: number | null;
  platformBreakdown: PlatformBreakdownItem[];
  topPosts: TopPostItem[];
}

/** GET /api/v1/dashboard/charts */
export interface DashboardChartPoint {
  date: string;
  reach: number;
  impressions: number;
  likes: number;
  comments: number;
  shares: number;
  engagementRate?: number | null;
}

export interface DashboardChartsResponse {
  brandProfileId: string;
  campaignId?: string | null;
  points: DashboardChartPoint[];
}

/** GET /api/v1/dashboard/activity */
export interface DashboardActivityItem {
  id: string;
  action: string;
  message?: string | null;
  entityType?: string | null;
  entityId?: string | null;
  createdAt: string;
}
