import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { CampaignService } from '../../../services/campaign.service';
import { BrandProfileService } from '../../../services/brand-profile.service';
import { ScheduledPostService } from '../../../services/scheduled-post.service';
import { AnalyticsService } from '../../../services/analytics.service';
import { DashboardService } from '../../../services/dashboard.service';
import {
  BACKEND_TO_CAMPAIGN_PLATFORM, BACKEND_TO_CAMPAIGN_STATUS, CAMPAIGN_PLATFORM_META,
  CAMPAIGN_STATUS_LABELS, CampaignPlatform, CampaignPlatformMeta, GetCampaignResponse,
  campaignObjectiveLabel,
} from '../../../model/campaign.model';
import { BackendSocialPlatform } from '../../../model/content-item.model';
import { CampaignAnalyticsSummary, metricAvailable, formatEngagementRate } from '../../../model/analytics.model';
import { DashboardChartPoint } from '../../../model/dashboard.model';
import { SeoService } from '../../../services/seo.service';
import { extractApiErrorMessage } from '../../../core/auth/api-error.util';
import { formatCampaignBudget, formatCampaignDateRange } from '../campaign-format.util';
import { CampaignEditModal } from '../campaign-edit-modal/campaign-edit-modal';
import { PermissionService } from '../../../core/tenant/permission.service';
import { BalanceChart } from '../../dashboard/balance-chart/balance-chart';

@Component({
  selector: 'app-campaign-detail-page',
  imports: [PageHeader, RouterLink, CampaignEditModal, BalanceChart],
  templateUrl: './campaign-detail-page.html',
  styleUrls: ['../../dashboard/dashboard-shared.css', './campaign-detail-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CampaignDetailPage {
  private readonly route = inject(ActivatedRoute);
  private readonly campaignService = inject(CampaignService);
  private readonly brandProfileService = inject(BrandProfileService);
  private readonly scheduledPostService = inject(ScheduledPostService);
  private readonly analyticsService = inject(AnalyticsService);
  private readonly dashboardService = inject(DashboardService);
  private readonly seo = inject(SeoService);
  protected readonly perms = inject(PermissionService);
  protected readonly formatEngagementRate = formatEngagementRate;

  /** `PUT /campaigns/{id}` existed and was fully wired, but nothing outside the onboarding
   *  wizard's autosave ever called it — so a campaign's name, dates, budget, objective and
   *  platforms could never be corrected once onboarding was done. */
  protected readonly editOpen = signal(false);

  /// Reactive route param — the router reuses this component instance across navigations that
  /// only change `:id`, so a snapshot read here would freeze on the first campaign forever.
  private readonly paramMap = toSignal(this.route.paramMap, { requireSync: true });
  protected readonly campaignId = computed(() => this.paramMap().get('id') ?? '');
  /** The one source of campaign data on this page. It used to also read a "summary" out of
   *  CampaignService's list for the name and status, which meant those two fields were blank on
   *  any deep link (the list is loaded once by UserLayout) even though this record has them. */
  protected readonly detail = signal<GetCampaignResponse | null>(null);
  protected readonly detailError = signal<string | null>(null);

  protected readonly brandProfile = computed(() => {
    const brandProfileId = this.detail()?.brandProfileId;
    return brandProfileId ? this.brandProfileService.getById(brandProfileId)() : undefined;
  });

  protected readonly platforms = computed<CampaignPlatform[]>(() => {
    const raw = this.detail()?.targetPlatforms ?? [];
    return raw
      .map(p => BACKEND_TO_CAMPAIGN_PLATFORM[p as BackendSocialPlatform])
      .filter((p): p is CampaignPlatform => !!p);
  });

  protected readonly objectiveLabel = computed(() => campaignObjectiveLabel(this.detail()?.objective));

  /** The campaign's own period and budget, formatted — these were rendered as raw API values
   *  (an ISO date, an unseparated number) directly in the template. */
  protected readonly dateRangeLabel = computed(() =>
    formatCampaignDateRange(this.detail()?.startDate, this.detail()?.endDate),
  );

  protected readonly budgetLabel = computed(() =>
    formatCampaignBudget(this.detail()?.budgetAmount, this.detail()?.budgetCurrency),
  );

  /** Status comes from the full detail record, not the list summary: the list is loaded once by
   *  UserLayout, so a deep link (or an archived campaign, which the list now hides) left the
   *  status pill missing entirely on a page that had the real status in hand all along. */
  protected readonly status = computed(() => {
    const backendStatus = this.detail()?.status;
    return backendStatus ? BACKEND_TO_CAMPAIGN_STATUS[backendStatus] : null;
  });

  protected readonly statusLabel = computed(() => {
    const status = this.status();
    return status ? CAMPAIGN_STATUS_LABELS[status] : '';
  });

  protected readonly platformMeta = CAMPAIGN_PLATFORM_META;
  protected readonly platformFilter = signal<CampaignPlatform | 'all'>('all');

  private readonly campaignPosts = computed(() => this.scheduledPostService.byCampaign(this.campaignId())());

  /** "Upcoming" = not yet published, optionally narrowed to one platform. */
  protected readonly upcomingPosts = computed(() => {
    const filter = this.platformFilter();
    return this.campaignPosts()
      .filter(p => p.status === 'scheduled')
      .filter(p => filter === 'all' || p.platform === filter)
      .sort((a, b) => a.scheduledAt.localeCompare(b.scheduledAt));
  });

  // ── Insights — real data from AnalyticsService, never a fabricated placeholder. ──
  protected readonly analytics = signal<CampaignAnalyticsSummary | null>(null);
  protected readonly analyticsLoading = signal(true);
  protected readonly analyticsError = signal<string | null>(null);

  /** Backend-ranked (by UniqueViewers) Top/Bottom Posts — previously this page re-sorted
   *  `analytics.posts` by Likes+Comments+Shares itself, duplicating ranking logic the backend
   *  already computes (and disagreeing with it: the API's TopPosts/BottomPosts are ranked by
   *  UniqueViewers, per analytics-spec.md §7). Reusing the API's lists directly instead. */
  protected readonly topPosts = computed(() => this.analytics()?.topPosts ?? []);
  protected readonly bottomPosts = computed(() => this.analytics()?.bottomPosts ?? []);

  // ── Historical/chart data — reuses the existing GetDashboardCharts contract + BalanceChart
  // component (already used by the CRM dashboard page) scoped to this campaign, rather than
  // building a bespoke chart or a new backend endpoint. ──
  protected readonly chartPoints = signal<DashboardChartPoint[]>([]);
  protected readonly chartDays = signal(30);

  constructor() {
    effect(() => {
      // Read from the fetched detail, not the list summary — on a deep link the list hasn't
      // loaded yet, so the page title used to drop the campaign name entirely.
      const name = this.detail()?.name;
      const id = this.campaignId();
      this.seo.setPageSeo({
        title: (name ? name + ' | ' : '') + 'الحملات | رواج',
        description: 'تفاصيل الحملة: الإحصائيات، الأداء، والمنشورات القادمة.',
        keywords: 'رواج, تفاصيل الحملة, أداء الحملة, إحصائيات',
        path: '/dashboard/campaigns/' + id,
        image: '/home-hero-light.png',
        type: 'website',
        noIndex: true,
      });
    });

    // Re-fetches everything whenever the route's campaign id actually changes — the router
    // reuses this component instance across in-app navigation between two campaigns' detail
    // pages, so without this the page keeps showing the first campaign forever.
    effect(() => {
      const id = this.campaignId();
      this.detail.set(null);
      this.detailError.set(null);
      if (!id) return;

      this.campaignService.getCampaign(id).subscribe({
        next: res => {
          if (res.data) this.detail.set(res.data);
          else this.detailError.set('لم يتم العثور على الحملة.');
        },
        error: err => this.detailError.set(extractApiErrorMessage(err, 'تعذّر تحميل بيانات الحملة.')),
      });

      this.loadAnalytics(id);
    });

    // ScheduledPostService's list is otherwise only ever populated by /dashboard/calendar —
    // without this, "المنشورات القادمة" below is always empty.
    effect(() => {
      const brandProfileId = this.detail()?.brandProfileId;
      const id = this.campaignId();
      if (brandProfileId) this.scheduledPostService.refresh(brandProfileId, id).subscribe();
    });

    // Chart needs brandProfileId (only known once `detail` resolves) — re-requested whenever the
    // campaign changes or the user picks a different day range via the chart's own range chips.
    effect(() => {
      const brandProfileId = this.detail()?.brandProfileId;
      const id = this.campaignId();
      const days = this.chartDays();
      if (!brandProfileId || !id) return;
      this.dashboardService.getCharts(brandProfileId, id, days).subscribe({
        next: res => { if (res.data) this.chartPoints.set(res.data.points); },
        error: () => { /* the chart card just stays empty; not worth a blocking page-level error */ },
      });
    });
  }

  /** BalanceChart's range chips (1M/6M/1Y/ALL) re-request the chart with a different day-window,
   *  mirroring CrmPage's `onChartDaysChange`. */
  protected onChartDaysChange(days: number): void {
    this.chartDays.set(days);
  }

  private loadAnalytics(campaignId: string): void {
    // Reset first — the router reuses this component across campaigns, so without clearing these
    // the previous campaign's KPI tiles (or its error) stayed on screen while the new campaign's
    // analytics were still in flight.
    this.analytics.set(null);
    this.analyticsError.set(null);
    this.analyticsLoading.set(true);
    this.analyticsService.getCampaign(campaignId).subscribe({
      next: res => {
        this.analyticsLoading.set(false);
        if (res.data) this.analytics.set(res.data);
      },
      error: err => {
        this.analyticsLoading.set(false);
        this.analyticsError.set(extractApiErrorMessage(err, 'تعذّر تحميل إحصاءات الحملة.'));
      },
    });
  }

  protected uniqueViewersAvailable(): boolean {
    return this.analytics()?.uniqueViewersAvailable ?? metricAvailable(this.analytics()?.posts ?? [], 'uniqueViewers');
  }

  protected viewsAvailable(): boolean {
    return this.analytics()?.viewsAvailable ?? metricAvailable(this.analytics()?.posts ?? [], 'views');
  }

  protected engagementRateAvailable(): boolean {
    return this.analytics()?.engagementRateAvailable ?? metricAvailable(this.analytics()?.posts ?? [], 'engagementRate');
  }

  protected clicksAvailable(): boolean {
    return this.analytics()?.clicksAvailable ?? metricAvailable(this.analytics()?.posts ?? [], 'clicks');
  }

  protected platformMetaFor(p: CampaignPlatform): CampaignPlatformMeta {
    return CAMPAIGN_PLATFORM_META[p];
  }

  protected openEdit(): void {
    if (this.perms.canEdit()) this.editOpen.set(true);
  }

  /** The modal returns the saved record, so the page updates from the response rather than
   *  issuing another GET for data it was just handed. */
  protected onCampaignSaved(updated: GetCampaignResponse): void {
    this.detail.set(updated);
  }

  protected setPlatformFilter(p: CampaignPlatform | 'all'): void {
    this.platformFilter.set(p);
  }

  protected compact(n: number): string {
    if (n >= 1_000_000) return (n / 1_000_000).toFixed(1) + 'M';
    if (n >= 1_000) return (n / 1_000).toFixed(1) + 'K';
    return String(n);
  }

  protected formatDateTime(iso: string): string {
    return new Date(iso).toLocaleDateString('ar-EG', {
      day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit', hour12: true,
    });
  }
}
