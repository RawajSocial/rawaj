export type PlatformKey = 'all' | 'instagram' | 'facebook' | 'tiktok' | 'snapchat' | 'linkedin';

export interface PlatformStat {
  key: Exclude<PlatformKey, 'all'>;
  label: string;
  icon: string;
  color: string;
  followers: number;
  reach: number;
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
  change: number;
  accentColor: string;
}

export interface TopPost {
  id: string;
  platform: Exclude<PlatformKey, 'all'>;
  content: string;
  reach: number;
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

export interface MetaWidgetMetric {
  label: string;
  value: string;
}

export interface MetaWidget {
  label: string;
  icon: string;
  metrics: MetaWidgetMetric[];
}

/** Compact "1.2K" / "3.4M" formatting shared by every overview widget that
 *  displays a raw follower/reach/engagement count. */
export function compactNumber(n: number): string {
  if (n >= 1_000_000) return (n / 1_000_000).toFixed(1).replace(/\.0$/, '') + 'M';
  if (n >= 1_000) return (n / 1_000).toFixed(1).replace(/\.0$/, '') + 'K';
  return String(n);
}
