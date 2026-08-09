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

/** Phase 11 — Growth/Change-over-time (spec §3 P2): delta between a post's two most recent
 *  `PostAnalytics` snapshots for a single metric. Computed at read time from history the
 *  Post Dashboard already fetches (`GetPostAnalytics` returns full history, newest first) — no
 *  new backend endpoint or storage, per the spec's explicit "reads history directly" note. */
export interface MetricGrowth {
  delta: number | null;
  /** Percentage change relative to the prior value; null when the prior value is null/undefined
   *  or 0 (division by zero has no meaningful percentage). */
  percentChange: number | null;
}

export function computeMetricGrowth(current: number | null | undefined, prior: number | null | undefined): MetricGrowth {
  if (current === null || current === undefined || prior === null || prior === undefined) {
    return { delta: null, percentChange: null };
  }
  const delta = current - prior;
  return { delta, percentChange: prior !== 0 ? Math.round((delta / prior) * 1000) / 10 : null };
}

/** Phase 11 — Post vs. Campaign Average (spec §3 P1): a single post's Engagement Rate compared to
 *  its campaign's weighted Engagement Rate. A comparison only — both rates are already computed
 *  by the backend (`PostAnalyticsSnapshot.engagementRate` and
 *  `CampaignAnalyticsSummary.averageEngagementRate`), so this never re-derives either value. */
export interface PostVsCampaignAverage {
  postRate: number | null;
  campaignRate: number | null;
  /** Percentage-point difference (postRate - campaignRate), scaled the same way
   *  `formatEngagementRate` scales a single rate — e.g. 0.12 vs 0.08 -> 4.0 points. */
  deltaPoints: number | null;
}

export function comparePostToCampaignAverage(
  postRate: number | null | undefined,
  campaignRate: number | null | undefined,
): PostVsCampaignAverage {
  if (postRate === null || postRate === undefined || campaignRate === null || campaignRate === undefined) {
    return { postRate: postRate ?? null, campaignRate: campaignRate ?? null, deltaPoints: null };
  }
  return { postRate, campaignRate, deltaPoints: Math.round((postRate - campaignRate) * 1000) / 10 };
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
