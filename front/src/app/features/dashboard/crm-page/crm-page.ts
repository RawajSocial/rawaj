import { ChangeDetectionStrategy, Component, computed, signal } from '@angular/core';
import { KpiCard } from '../kpi-card/kpi-card';
import { BalanceChart } from '../balance-chart/balance-chart';
import { RadarChart } from '../radar-chart/radar-chart';
import { PageHeader } from '../../../shared/components/page-header/page-header';

type PlatformKey = 'all' | 'instagram' | 'facebook' | 'tiktok' | 'snapchat' | 'linkedin';

interface PlatformStat {
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

interface KpiData {
  title: string;
  value: string;
  icon: string;
  iconBg: string;
  iconColor: string;
  change: number;
  accentColor: string;
}

interface TopPost {
  id: string;
  platform: Exclude<PlatformKey, 'all'>;
  content: string;
  reach: number;
  engagement: number;
}

const PLATFORM_STATS: PlatformStat[] = [
  { key: 'instagram', label: 'إنستغرام', icon: 'fa-brands fa-instagram',  color: '#E1306C', followers: 48200, reach: 512000, engagementRate: 5.4, posts: 46, change: 12.4 },
  { key: 'tiktok',    label: 'تيك توك',  icon: 'fa-brands fa-tiktok',      color: '#010101', followers: 63500, reach: 890000, engagementRate: 7.8, posts: 31, change: 24.1 },
  { key: 'facebook',  label: 'فيسبوك',   icon: 'fa-brands fa-facebook-f', color: '#1877F2', followers: 29800, reach: 240000, engagementRate: 3.1, posts: 38, change: 4.6 },
  { key: 'snapchat',  label: 'سناب شات', icon: 'fa-brands fa-snapchat',    color: '#FFC800', followers: 15400, reach: 132000, engagementRate: 4.2, posts: 22, change: -1.8 },
  { key: 'linkedin',  label: 'لينكدإن',  icon: 'fa-brands fa-linkedin-in', color: '#0A66C2', followers: 8600,  reach: 61000,  engagementRate: 2.4, posts: 14, change: 6.9 },
];

@Component({
  selector: 'app-crm-page',
  standalone: true,
  imports: [KpiCard, BalanceChart, RadarChart, PageHeader],
  templateUrl: './crm-page.html',
  styleUrl: './crm-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CrmPage {
  protected readonly platforms = PLATFORM_STATS;
  protected readonly platformFilter = signal<PlatformKey>('all');

  protected readonly filterTabs: { key: PlatformKey; label: string; icon: string }[] = [
    { key: 'all', label: 'كل المنصات', icon: 'fa-layer-group' },
    ...PLATFORM_STATS.map(p => ({ key: p.key as PlatformKey, label: p.label, icon: p.icon })),
  ];

  /** Aggregate or single-platform stats depending on the active filter. */
  private readonly activeStats = computed(() => {
    const f = this.platformFilter();
    if (f !== 'all') return PLATFORM_STATS.find(p => p.key === f)!;
    const sum = PLATFORM_STATS.reduce(
      (acc, p) => ({
        followers: acc.followers + p.followers,
        reach: acc.reach + p.reach,
        posts: acc.posts + p.posts,
        engWeighted: acc.engWeighted + p.engagementRate * p.reach,
      }),
      { followers: 0, reach: 0, posts: 0, engWeighted: 0 },
    );
    return {
      followers: sum.followers,
      reach: sum.reach,
      posts: sum.posts,
      engagementRate: +(sum.engWeighted / sum.reach).toFixed(1),
      change: 11.2,
    };
  });

  protected readonly kpis = computed<KpiData[]>(() => {
    const s = this.activeStats();
    return [
      { title: 'إجمالي المتابعين', value: this.compact(s.followers),   icon: 'fa-users',       iconBg: 'rgba(124,58,237,0.12)', iconColor: '#7C3AED', change: s.change,  accentColor: '#7C3AED' },
      { title: 'مدى الوصول الشهري', value: this.compact(s.reach),       icon: 'fa-bullseye',    iconBg: 'rgba(37,99,235,0.12)',  iconColor: '#2563EB', change: 18.6,      accentColor: '#2563EB' },
      { title: 'معدل التفاعل',       value: s.engagementRate + '%',      icon: 'fa-heart',       iconBg: 'rgba(236,72,153,0.12)', iconColor: '#EC4899', change: 2.3,       accentColor: '#EC4899' },
      { title: 'المنشورات المنشورة', value: String(s.posts),             icon: 'fa-paper-plane', iconBg: 'rgba(22,163,74,0.12)',  iconColor: '#16A34A', change: 5.1,       accentColor: '#16A34A' },
    ];
  });

  /** Platform reach share for the breakdown bars. */
  protected readonly totalReach = computed(() => PLATFORM_STATS.reduce((a, p) => a + p.reach, 0));

  protected reachShare(p: PlatformStat): number {
    return Math.round((p.reach / this.totalReach()) * 100);
  }

  protected readonly topPosts: TopPost[] = [
    { id: 't1', platform: 'tiktok',    content: 'تحدي الصيف — شارك مقطعك وستظهر على صفحتنا 🌊', reach: 312000, engagement: 41200 },
    { id: 't2', platform: 'instagram', content: 'منتجنا الجديد وصل أخيراً! كن أول من يجربه 🎉',   reach: 128000, engagement: 15400 },
    { id: 't3', platform: 'facebook',  content: 'عروض رمضان لا تفوتك! تسوق الآن بأفضل الأسعار',   reach: 96000,  engagement: 7200 },
    { id: 't4', platform: 'snapchat',  content: 'قصة حصرية: كواليس إطلاق منتجنا الجديد 👻',       reach: 54000,  engagement: 4800 },
  ];

  protected platformCfg(key: Exclude<PlatformKey, 'all'>): PlatformStat {
    return PLATFORM_STATS.find(p => p.key === key)!;
  }

  protected setFilter(key: PlatformKey): void {
    this.platformFilter.set(key);
  }

  protected compact(n: number): string {
    if (n >= 1_000_000) return (n / 1_000_000).toFixed(1).replace(/\.0$/, '') + 'M';
    if (n >= 1_000) return (n / 1_000).toFixed(1).replace(/\.0$/, '') + 'K';
    return String(n);
  }
}
