import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { KpiCard } from '../kpi-card/kpi-card';
import { BalanceChart } from '../balance-chart/balance-chart';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { ConnectAccountsCard } from './connect-accounts-card/connect-accounts-card';
import { PlatformPerformanceCard } from './platform-performance-card/platform-performance-card';
import { TopPostsCard } from './top-posts-card/top-posts-card';
import { MetaAnalyticsCard } from './meta-analytics-card/meta-analytics-card';
import { SeoService } from '../../../services/seo.service';
import { KpiData, MetaWidget, PlatformKey, PlatformStat, TopPost, TopPostView, compactNumber } from './crm-page.model';

const PLATFORM_STATS: PlatformStat[] = [
  { key: 'instagram', label: 'إنستغرام', icon: 'fa-brands fa-instagram',  color: 'var(--color-instagram)', followers: 48200, reach: 512000, engagementRate: 5.4, posts: 46, change: 12.4 },
  { key: 'tiktok',    label: 'تيك توك',  icon: 'fa-brands fa-tiktok',      color: 'var(--color-tiktok)',    followers: 63500, reach: 890000, engagementRate: 7.8, posts: 31, change: 24.1 },
  { key: 'facebook',  label: 'فيسبوك',   icon: 'fa-brands fa-facebook-f', color: 'var(--color-facebook)',  followers: 29800, reach: 240000, engagementRate: 3.1, posts: 38, change: 4.6 },
  { key: 'snapchat',  label: 'سناب شات', icon: 'fa-brands fa-snapchat',    color: 'var(--color-snapchat)',  followers: 15400, reach: 132000, engagementRate: 4.2, posts: 22, change: -1.8 },
  { key: 'linkedin',  label: 'لينكدإن',  icon: 'fa-brands fa-linkedin-in', color: 'var(--color-linkedin)',  followers: 8600,  reach: 61000,  engagementRate: 2.4, posts: 14, change: 6.9 },
];

@Component({
  selector: 'app-crm-page',
  standalone: true,
  imports: [KpiCard, BalanceChart, PageHeader, ConnectAccountsCard, PlatformPerformanceCard, TopPostsCard, MetaAnalyticsCard],
  templateUrl: './crm-page.html',
  styleUrl: './crm-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CrmPage {
  private readonly seo = inject(SeoService);

  constructor() {
    this.seo.setPageSeo({
      title: 'التحليلات والنظرة العامة | رواج',
      description: 'تابع أداء حساباتك على منصات التواصل الاجتماعي ونتائج حملاتك من مكان واحد.',
      keywords: 'رواج, تحليلات, أداء الحسابات, لوحة التحكم',
      path: '/dashboard',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });
  }

  protected readonly platforms = PLATFORM_STATS;
  protected readonly platformFilter = signal<PlatformKey>('all');

  protected readonly filterTabs: { key: PlatformKey; label: string; icon: string; color: string }[] = [
    { key: 'all', label: 'كل المنصات', icon: 'fa-layer-group', color: 'var(--color-dark)' },
    ...PLATFORM_STATS.map(p => ({ key: p.key as PlatformKey, label: p.label, icon: p.icon, color: p.color })),
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
      { title: 'إجمالي المتابعين', value: compactNumber(s.followers),   icon: 'fa-users',       iconBg: 'rgb(94 0 255 / 12%)', iconColor: '#5e00ff', change: s.change,  accentColor: '#5e00ff' },
      { title: 'مدى الوصول الشهري', value: compactNumber(s.reach),       icon: 'fa-bullseye',    iconBg: 'rgba(37,99,235,0.12)',  iconColor: '#0050ff', change: 18.6,      accentColor: '#0050ff' },
      { title: 'معدل التفاعل',       value: s.engagementRate + '%',      icon: 'fa-heart',       iconBg: 'rgb(255 0 126 / 12%)', iconColor: '#ff007e', change: 2.3,       accentColor: '#EC4899' },
      { title: 'المنشورات المنشورة', value: String(s.posts),             icon: 'fa-paper-plane', iconBg: 'rgb(0 255 94 / 12%)',  iconColor: '#00f85c', change: 5.1,       accentColor: '#00f85c' },
    ];
  });

  protected readonly metaWidgets: MetaWidget[] = [
    {
      label: 'إعلانات Meta',
      icon: 'fa-brands fa-meta',
      metrics: [
        { label: 'الإنفاق الإعلاني', value: '$1,240' },
        { label: 'الظهور (Impressions)', value: '486K' },
        { label: 'تكلفة الألف ظهور', value: '$8.4' },
        { label: 'العائد على الإنفاق', value: '3.2x' },
      ],
    },
    {
      label: 'رؤى إنستغرام',
      icon: 'fa-brands fa-instagram',
      metrics: [
        { label: 'زيارات الملف الشخصي', value: '9.6K' },
        { label: 'تفاعل الستوري', value: '4.1%' },
        { label: 'نقرات الرابط', value: '512' },
        { label: 'المتابعون الجدد', value: '+327' },
      ],
    },
  ];

  private readonly topPosts: TopPost[] = [
    { id: 't1', platform: 'tiktok',    content: 'تحدي الصيف — شارك مقطعك وستظهر على صفحتنا 🌊', reach: 312000, engagement: 41200 },
    { id: 't2', platform: 'instagram', content: 'منتجنا الجديد وصل أخيراً! كن أول من يجربه 🎉',   reach: 128000, engagement: 15400 },
    { id: 't3', platform: 'facebook',  content: 'عروض رمضان لا تفوتك! تسوق الآن بأفضل الأسعار',   reach: 96000,  engagement: 7200 },
    { id: 't4', platform: 'snapchat',  content: 'قصة حصرية: كواليس إطلاق منتجنا الجديد 👻',       reach: 54000,  engagement: 4800 },
  ];

  /** Top posts with each post's platform icon/color already resolved, so
   *  the display component doesn't need the full platform list too. */
  protected readonly topPostsView = computed<TopPostView[]>(() =>
    this.topPosts.map(post => {
      const cfg = PLATFORM_STATS.find(p => p.key === post.platform)!;
      return { ...post, icon: cfg.icon, color: cfg.color };
    }),
  );

  protected setFilter(key: PlatformKey): void {
    this.platformFilter.set(key);
  }
}
