import { BackendSocialPlatform } from './content-item.model';

/** Rawaj.Application.Features.Analytics shared type, reused by DashboardOverviewResponse. */
export interface PlatformBreakdownItem {
  platform: BackendSocialPlatform;
  uniqueViewers: number;
  views: number;
  followerCount: number;
}

/** Rawaj.Application.Features.Analytics shared type, reused by DashboardOverviewResponse. */
export interface TopPostItem {
  scheduledPostId: string;
  contentItemId: string;
  campaignId?: string | null;
  platform: BackendSocialPlatform;
  title?: string | null;
  content: string;
  uniqueViewers: number;
  likes: number;
  engagementRate?: number | null;
}

/** GET /api/v1/dashboard/overview */
export interface DashboardOverviewResponse {
  brandProfileId: string;
  campaignId?: string | null;
  postsTracked: number;
  totalViews: number;
  totalUniqueViewers: number;
  totalLikes: number;
  totalComments: number;
  totalShares: number;
  averageEngagementRate?: number | null;
  platformBreakdown: PlatformBreakdownItem[];
  topPosts: TopPostItem[];
  /** Backend record (Phase 6) also carries these — the frontend interface never picked them up
   *  until Phase 9. Not yet consumed by any component; added here for contract completeness. */
  bottomPosts: TopPostItem[];
  viewsAvailable: boolean;
  uniqueViewersAvailable: boolean;
  engagementRateAvailable: boolean;
  /** Percent change vs. the value as of the start of the current calendar month ("as of last
   *  month") — null when there's nothing to compare against yet, not a genuine 0% change. Render
   *  null as "—", never as "0%". */
  postsTrackedChangePercent?: number | null;
  totalUniqueViewersChangePercent?: number | null;
  averageEngagementRateChangePercent?: number | null;
  totalFollowersChangePercent?: number | null;
}

/** GET /api/v1/dashboard/charts */
export interface DashboardChartPoint {
  date: string;
  uniqueViewers: number;
  views: number;
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
