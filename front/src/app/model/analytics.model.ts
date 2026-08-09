import { BackendSocialPlatform } from './content-item.model';

/** Analytics/PostAnalyticsSnapshot.cs — returned by both SyncPostAnalytics and GetPostAnalytics. */
export interface PostAnalyticsSnapshot {
  id: string;
  scheduledPostId: string;
  platform: BackendSocialPlatform;
  recordedAt: string;
  views: number | null;
  uniqueViewers: number | null;
  likes: number | null;
  comments: number | null;
  shares: number | null;
  saves: number | null;
  clicks: number | null;
  engagementRate: number | null;
}

/** Analytics/GetBrandAnalytics/BrandAnalyticsOverview.cs's `TopPostItem` — shared by
 *  CampaignAnalyticsSummary/BrandAnalyticsOverview/DashboardOverviewResponse's Top/Bottom Posts
 *  lists. Distinct from (and not to be confused with) `dashboard.model.ts`'s older, differently
 *  shaped same-named type used by the CRM page's own top-posts card. */
export interface TopPostItem {
  scheduledPostId: string;
  contentItemId: string;
  campaignId?: string | null;
  platform: BackendSocialPlatform;
  title?: string | null;
  content: string;
  views: number;
  uniqueViewers: number;
  likes: number;
  engagementRate: number | null;
}

/** GET /api/v1/analytics/campaigns/{id} — CampaignAnalyticsSummary. */
export interface CampaignAnalyticsSummary {
  campaignId: string;
  postsTracked: number;
  /** Summed with `?? 0` server-side — check the matching *Available flag before trusting this,
   *  never treat it as "0 = no data" on its own. */
  totalViews: number;
  totalUniqueViewers: number;
  totalLikes: number;
  totalComments: number;
  totalShares: number;
  totalClicks: number;
  /** Likes + Comments + Shares summed across the campaign's latest-per-post snapshots — computed
   *  once on the backend (GetCampaignAnalyticsQueryHandler), never re-derived here. */
  totalEngagements: number;
  averageEngagementRate: number | null;
  posts: PostAnalyticsSnapshot[];
  topPosts: TopPostItem[];
  bottomPosts: TopPostItem[];
  viewsAvailable: boolean;
  uniqueViewersAvailable: boolean;
  engagementRateAvailable: boolean;
  clicksAvailable: boolean;
}

/** True only when at least one tracked post actually reported the metric. Use this (or the
 *  backend's *Available flags on CampaignAnalyticsSummary) instead of checking a total for zero —
 *  an unsupported metric is summed as 0 server-side and would otherwise look like real data. */
export function metricAvailable(
  posts: PostAnalyticsSnapshot[],
  key: 'views' | 'uniqueViewers' | 'engagementRate' | 'saves' | 'clicks',
): boolean {
  return posts.some(p => p[key] !== null && p[key] !== undefined);
}

/** `PostAnalyticsSnapshot.engagementRate` (and the campaign/brand/dashboard equivalents) are
 *  fractions straight from the backend's `decimal(5,4)` column (e.g. 0.1 for a 10% rate) — never
 *  pre-multiplied into a percentage. Callers must scale before appending "%"; use this instead of
 *  `rate + '%'`, which would render "0.1%" for what is actually a 10% rate. Rounded to one decimal
 *  place. */
export function formatEngagementRate(rate: number | null | undefined): string {
  if (rate === null || rate === undefined) return '—';
  return `${Math.round(rate * 1000) / 10}%`;
}
