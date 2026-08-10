import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { KpiCard } from '../kpi-card/kpi-card';
import { BalanceChart } from '../balance-chart/balance-chart';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { PlatformPerformanceCard } from './platform-performance-card/platform-performance-card';
import { TopPostsCard } from './top-posts-card/top-posts-card';
import { SeoService } from '../../../services/seo.service';
import { TenantService } from '../../../core/tenant/tenant.service';
import { BrandContextService } from '../../../services/brand-context.service';
import { DashboardService } from '../../../services/dashboard.service';
import { SocialAccountService } from '../../../core/social/social-account.service';
import { BackendSocialPlatform } from '../../../model/content-item.model';
import { formatEngagementRate } from '../../../model/analytics.model';
import { KpiData, PlatformKey, PlatformStat, TopPostView, compactNumber } from './crm-page.model';

const PLATFORM_CFG: Record<Exclude<PlatformKey, 'all'>, { label: string; icon: string; color: string }> = {
  instagram: { label: 'إنستغرام', icon: 'fa-brands fa-instagram',  color: 'var(--color-instagram)' },
  facebook:  { label: 'فيسبوك',   icon: 'fa-brands fa-facebook-f', color: 'var(--color-facebook)' },
};

const BACKEND_PLATFORM_TO_KEY: Record<BackendSocialPlatform, Exclude<PlatformKey, 'all'>> = {
  Instagram: 'instagram',
  Facebook: 'facebook',
};

@Component({
  selector: 'app-crm-page',
  standalone: true,
  imports: [RouterLink, KpiCard, BalanceChart, PageHeader, PlatformPerformanceCard, TopPostsCard],
  templateUrl: './crm-page.html',
  styleUrl: './crm-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CrmPage {
  private readonly seo = inject(SeoService);
  private readonly tenantService = inject(TenantService);
  protected readonly brandContextService = inject(BrandContextService);
  protected readonly dashboardService = inject(DashboardService);
  private readonly socialAccountService = inject(SocialAccountService);

  protected readonly isActivated = this.tenantService.isActivated;
  protected readonly brandProfileCount = this.tenantService.brandProfileCount;

  /** Gates the "connect social accounts" quick-action card — only worth prompting when the brand
   *  genuinely has none connected yet, rather than showing it unconditionally forever. */
  protected readonly hasConnectedAccounts = signal(false);

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

    // BrandContextService (fed by the header) is the dashboard's only source of brand/campaign
    // selection — changing either refetches every widget below.
    effect(() => {
      const brandId = this.brandContextService.selectedBrandProfileId();
      const campaignId = this.brandContextService.selectedCampaignId();
      if (!brandId) return;
      this.dashboardService.refresh(brandId, campaignId).subscribe();
    });

    effect(() => {
      const brandId = this.brandContextService.selectedBrandProfileId();
      if (!brandId) return;
      this.socialAccountService.getByBrand(brandId).subscribe(res => {
        this.hasConnectedAccounts.set((res.data?.length ?? 0) > 0);
      });
    });
  }

  protected readonly platformFilter = signal<PlatformKey>('all');

  /** Per-platform stats derived from the overview's platform breakdown. `followerCount` is a
   *  synced snapshot from the platform (see FollowerCountSyncer/AnalyticsSyncHostedService on the
   *  backend, refreshed roughly every 24h) rather than a live read, so it can lag briefly right
   *  after connecting a new account. Historical comparison ("change") still doesn't exist yet. */
  protected readonly platforms = computed<PlatformStat[]>(() => {
    const overview = this.dashboardService.overview();
    if (!overview) return [];
    return overview.platformBreakdown.map(p => {
      const key = BACKEND_PLATFORM_TO_KEY[p.platform];
      const cfg = PLATFORM_CFG[key];
      return {
        key,
        label: cfg.label,
        icon: cfg.icon,
        color: cfg.color,
        followers: p.followerCount,
        uniqueViewers: p.uniqueViewers,
        engagementRate: overview.averageEngagementRate ?? 0,
        posts: this.dashboardService.recentContent().filter(c => BACKEND_PLATFORM_TO_KEY[c.platform] === key).length,
        change: 0,
      };
    });
  });

  protected readonly filterTabs = computed<{ key: PlatformKey; label: string; icon: string; color: string }[]>(() => [
    { key: 'all', label: 'كل المنصات', icon: 'fa-layer-group', color: 'var(--color-dark)' },
    ...this.platforms().map(p => ({ key: p.key as PlatformKey, label: p.label, icon: p.icon, color: p.color })),
  ]);

  /** Aggregate or single-platform stats depending on the active filter. Phase 11: the "all
   *  platforms" branch used to re-derive its own uniqueViewers-weighted engagement rate from the
   *  per-platform list — duplicating a formula the backend already computes correctly
   *  (`PostAnalyticsAggregation.WeightedEngagementRate`, SUM(Engagements)/SUM(Views) across every
   *  post) and, worse, rounding it to a coarse single decimal along the way. It now consumes
   *  `overview.averageEngagementRate` directly instead of recomputing it. */
  private readonly activeStats = computed(() => {
    const f = this.platformFilter();
    const list = this.platforms();
    if (f !== 'all') return list.find(p => p.key === f);
    const overview = this.dashboardService.overview();
    const sum = list.reduce(
      (acc, p) => ({
        followers: acc.followers + p.followers,
        uniqueViewers: acc.uniqueViewers + p.uniqueViewers,
        posts: acc.posts + p.posts,
      }),
      { followers: 0, uniqueViewers: 0, posts: 0 },
    );
    return {
      followers: sum.followers,
      uniqueViewers: sum.uniqueViewers,
      posts: sum.posts,
      engagementRate: overview?.averageEngagementRate ?? 0,
      change: 0,
    };
  });

  protected readonly kpis = computed<KpiData[]>(() => {
    const overview = this.dashboardService.overview();
    const s = this.activeStats();
    return [
      { title: 'إجمالي المتابعين', value: compactNumber(s?.followers ?? 0), icon: 'fa-users',       iconBg: 'rgb(94 0 255 / 12%)',  iconColor: '#5e00ff', change: overview?.totalFollowersChangePercent ?? null, accentColor: '#5e00ff' },
      { title: 'المشاهدون الفريدون شهريًا', value: compactNumber(s?.uniqueViewers ?? overview?.totalUniqueViewers ?? 0), icon: 'fa-bullseye', iconBg: 'rgba(37,99,235,0.12)', iconColor: '#0050ff', change: overview?.totalUniqueViewersChangePercent ?? null, accentColor: '#0050ff' },
      { title: 'معدل التفاعل',       value: formatEngagementRate(overview?.averageEngagementRate), icon: 'fa-heart',       iconBg: 'rgb(255 0 126 / 12%)', iconColor: '#ff007e', change: overview?.averageEngagementRateChangePercent ?? null, accentColor: '#EC4899' },
      { title: 'المنشورات المنشورة', value: String(overview?.postsTracked ?? 0),           icon: 'fa-paper-plane', iconBg: 'rgb(0 255 94 / 12%)',  iconColor: '#00f85c', change: overview?.postsTrackedChangePercent ?? null, accentColor: '#00f85c' },
    ];
  });


  protected readonly topPostsView = computed<TopPostView[]>(() => {
    const overview = this.dashboardService.overview();
    if (!overview) return [];
    return overview.topPosts.map(post => {
      const key = BACKEND_PLATFORM_TO_KEY[post.platform];
      const cfg = PLATFORM_CFG[key];
      return {
        id: post.scheduledPostId,
        campaignId: post.campaignId,
        platform: key,
        content: post.title ?? post.content,
        uniqueViewers: post.uniqueViewers,
        engagement: Math.round((post.uniqueViewers * (post.engagementRate ?? 0)) / 100) || post.likes,
        icon: cfg.icon,
        color: cfg.color,
      };
    });
  });

  protected setFilter(key: PlatformKey): void {
    this.platformFilter.set(key);
  }

  /** BalanceChart's range chips (1M/6M/1Y/ALL) re-request the charts widget with a different
   *  day-window — everything else on the dashboard stays as-is. */
  protected onChartDaysChange(days: number): void {
    const brandId = this.brandContextService.selectedBrandProfileId();
    const campaignId = this.brandContextService.selectedCampaignId();
    if (!brandId) return;
    this.dashboardService.refresh(brandId, campaignId, days).subscribe();
  }
}
