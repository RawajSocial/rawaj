import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { CampaignService } from '../../../services/campaign.service';
import { BrandProfileService } from '../../../services/brand-profile.service';
import { ScheduledPostService } from '../../../services/scheduled-post.service';
import { AnalyticsService } from '../../../services/analytics.service';
import { CampaignObjective, CampaignPlatform, CampaignStatus, GetCampaignResponse } from '../../../model/campaign.model';
import { BackendSocialPlatform } from '../../../model/content-item.model';
import { CampaignAnalyticsSummary, metricAvailable } from '../../../model/analytics.model';
import { SeoService } from '../../../services/seo.service';
import { extractApiErrorMessage } from '../../../core/auth/api-error.util';

interface PlatformMeta {
  icon: string;
  color: string;
  label: string;
}

const PLATFORM_META: Record<CampaignPlatform, PlatformMeta> = {
  instagram: { icon: 'fa-brands fa-instagram',   color: 'var(--color-instagram)', label: 'إنستغرام' },
  facebook:  { icon: 'fa-brands fa-facebook-f',  color: 'var(--color-facebook)',  label: 'فيسبوك' },
  tiktok:    { icon: 'fa-brands fa-tiktok',      color: 'var(--color-tiktok)',    label: 'تيك توك' },
  youtube:   { icon: 'fa-brands fa-youtube',     color: 'var(--color-youtube)',   label: 'يوتيوب' },
  x:         { icon: 'fa-brands fa-x-twitter',   color: 'var(--color-x)',         label: 'إكس' },
  snapchat:  { icon: 'fa-brands fa-snapchat',    color: 'var(--color-snapchat)',  label: 'سناب شات' },
  linkedin:  { icon: 'fa-brands fa-linkedin-in', color: 'var(--color-linkedin)',  label: 'لينكد إن' },
};

/** GetCampaignResponse.targetPlatforms comes back as the backend SocialPlatform enum's PascalCase
 *  name (see CreateCampaignCommandHandler: `Enum.Parse<SocialPlatform>(p, true).ToString()`), not
 *  the frontend's lowercase CampaignPlatform — this bridges the two, same mapping as
 *  ScheduledPostService.PLATFORM_MAP. */
const BACKEND_TO_FRONT_PLATFORM: Record<BackendSocialPlatform, CampaignPlatform> = {
  Instagram: 'instagram',
  Facebook: 'facebook',
  Tiktok: 'tiktok',
  Youtube: 'youtube',
  Twitter: 'x',
  Linkedin: 'linkedin',
};

const OBJECTIVE_LABELS: Record<CampaignObjective, string> = {
  awareness: 'الوعي بالعلامة', traffic: 'زيارات الموقع', engagement: 'التفاعل', leads: 'توليد عملاء', sales: 'رفع المبيعات',
};

const STATUS_LABELS: Record<CampaignStatus, string> = {
  active: 'نشطة', paused: 'موقوفة', completed: 'مكتملة', draft: 'مسودة', archived: 'مؤرشفة',
};

@Component({
  selector: 'app-campaign-detail-page',
  imports: [PageHeader, RouterLink],
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
  private readonly seo = inject(SeoService);

  /// Reactive route param — the router reuses this component instance across navigations that
  /// only change `:id`, so a snapshot read here would freeze on the first campaign forever.
  private readonly paramMap = toSignal(this.route.paramMap, { requireSync: true });
  protected readonly campaignId = computed(() => this.paramMap().get('id') ?? '');
  /** The list-derived summary (name, status) — always already loaded by user-layout. */
  protected readonly campaignSummary = computed(() => this.campaignService.getById(this.campaignId())());
  /** The full detail record (objective, platforms, budget, planApprovedAt) — the list summary
   *  doesn't carry these, so they're fetched directly rather than defaulted. */
  protected readonly detail = signal<GetCampaignResponse | null>(null);
  protected readonly detailError = signal<string | null>(null);

  protected readonly brandProfile = computed(() => {
    const brandProfileId = this.detail()?.brandProfileId;
    return brandProfileId ? this.brandProfileService.getById(brandProfileId)() : undefined;
  });

  protected readonly platforms = computed<CampaignPlatform[]>(() => {
    const raw = this.detail()?.targetPlatforms ?? [];
    return raw
      .map(p => BACKEND_TO_FRONT_PLATFORM[p as BackendSocialPlatform])
      .filter((p): p is CampaignPlatform => !!p);
  });

  protected readonly objectiveLabel = computed(() => {
    const objective = this.detail()?.objective?.toLowerCase() as CampaignObjective | undefined;
    return objective && OBJECTIVE_LABELS[objective] ? OBJECTIVE_LABELS[objective] : (this.detail()?.objective ?? '—');
  });

  protected readonly platformMeta = PLATFORM_META;
  protected readonly statusLabels = STATUS_LABELS;
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

  protected readonly topPosts = computed(() => {
    const posts = this.analytics()?.posts ?? [];
    return [...posts]
      .sort((a, b) => (b.likes ?? 0) + (b.comments ?? 0) + (b.shares ?? 0) - ((a.likes ?? 0) + (a.comments ?? 0) + (a.shares ?? 0)))
      .slice(0, 5);
  });

  constructor() {
    effect(() => {
      const c = this.campaignSummary();
      const id = this.campaignId();
      this.seo.setPageSeo({
        title: (c ? c.name + ' | ' : '') + 'الحملات | رواج',
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
  }

  private loadAnalytics(campaignId: string): void {
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

  protected reachAvailable(): boolean {
    return this.analytics()?.reachAvailable ?? metricAvailable(this.analytics()?.posts ?? [], 'reach');
  }

  protected impressionsAvailable(): boolean {
    return this.analytics()?.impressionsAvailable ?? metricAvailable(this.analytics()?.posts ?? [], 'impressions');
  }

  protected engagementRateAvailable(): boolean {
    return this.analytics()?.engagementRateAvailable ?? metricAvailable(this.analytics()?.posts ?? [], 'engagementRate');
  }

  protected platformMetaFor(p: CampaignPlatform): PlatformMeta {
    return PLATFORM_META[p];
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
    return new Date(iso).toLocaleDateString('ar-SA', {
      day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit', hour12: true,
    });
  }
}
