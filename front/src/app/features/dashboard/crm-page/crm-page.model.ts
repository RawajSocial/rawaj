export type PlatformKey = 'all' | 'instagram' | 'facebook';

export interface PlatformStat {
  key: Exclude<PlatformKey, 'all'>;
  label: string;
  icon: string;
  color: string;
  followers: number;
  uniqueViewers: number;
  engagementRate: number;
  posts: number;
  change: number;
}

export interface KpiData {
  title: string;
  value: string;
  icon: string;
  iconBg: string;
  iconColor: string;
  /** null when there's no prior-period value to compare against yet — render as "—", never "0%". */
  change: number | null;
  accentColor: string;
}

export interface TopPost {
  id: string;
  /** The campaign this post belongs to, when it has one — lets the card link through to the
   *  post's detail page (standalone, non-campaign posts have no detail route to link to). */
  campaignId?: string | null;
  platform: Exclude<PlatformKey, 'all'>;
  content: string;
  uniqueViewers: number;
  engagement: number;
}

/** A TopPost with its platform icon/color already resolved, so the display
 *  component stays presentational and doesn't need the full platform list. */
export interface TopPostView extends TopPost {
  icon: string;
  color: string;
}

export interface ConnectPlatform {
  key: string;
  label: string;
  icon: string;
  color: string;
}

/** Compact "1.2K" / "3.4M" formatting shared by every overview widget that
 *  displays a raw follower/unique-viewer/engagement count. */
export function compactNumber(n: number): string {
  if (n >= 1_000_000) return (n / 1_000_000).toFixed(1).replace(/\.0$/, '') + 'M';
  if (n >= 1_000) return (n / 1_000).toFixed(1).replace(/\.0$/, '') + 'K';
  return String(n);
}
