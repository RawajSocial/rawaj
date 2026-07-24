import { BackendSocialPlatform } from './content-item.model';

/** Analytics/PostAnalyticsSnapshot.cs — returned by both SyncPostAnalytics and GetPostAnalytics. */
export interface PostAnalyticsSnapshot {
  id: string;
  scheduledPostId: string;
  platform: BackendSocialPlatform;
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

/** GET /api/v1/analytics/campaigns/{id} — CampaignAnalyticsSummary. */
export interface CampaignAnalyticsSummary {
  campaignId: string;
  postsTracked: number;
  /** Summed with `?? 0` server-side — check the matching *Available flag before trusting this,
   *  never treat it as "0 = no data" on its own. */
  totalImpressions: number;
  totalReach: number;
  totalLikes: number;
  totalComments: number;
  totalShares: number;
  averageEngagementRate: number | null;
  posts: PostAnalyticsSnapshot[];
  impressionsAvailable: boolean;
  reachAvailable: boolean;
  engagementRateAvailable: boolean;
}

/** True only when at least one tracked post actually reported the metric. Use this (or the
 *  backend's *Available flags on CampaignAnalyticsSummary) instead of checking a total for zero —
 *  an unsupported metric is summed as 0 server-side and would otherwise look like real data. */
export function metricAvailable(
  posts: PostAnalyticsSnapshot[],
  key: 'impressions' | 'reach' | 'engagementRate' | 'saves' | 'clicks',
): boolean {
  return posts.some(p => p[key] !== null && p[key] !== undefined);
}
