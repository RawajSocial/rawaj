import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { CampaignService } from '../../../services/campaign.service';
import { ScheduledPostService } from '../../../services/scheduled-post.service';
import { AnalyticsService } from '../../../services/analytics.service';
import { CoinPricingService } from '../../../services/coin-pricing.service';
import { PermissionService } from '../../../core/tenant/permission.service';
import { TooltipDirective } from '../../../shared/directives/tooltip.directive';
import { CAMPAIGN_PLATFORM_META, GetCampaignResponse } from '../../../model/campaign.model';
import { PostStatus } from '../../../model/scheduled-post.model';
import {
  PostAnalyticsSnapshot, metricAvailable, formatEngagementRate,
  MetricGrowth, computeMetricGrowth, PostVsCampaignAverage, comparePostToCampaignAverage,
  CampaignAnalyticsSummary,
} from '../../../model/analytics.model';
import { SeoService } from '../../../services/seo.service';
import { ErrorModalService } from '../../../services/error-modal.service';
import { ConfirmDialogService } from '../../../services/confirm-dialog.service';
import { extractApiErrorMessage } from '../../../core/auth/api-error.util';

const STATUS_META: Record<PostStatus, { label: string; color: string }> = {
  scheduled: { label: 'مجدول', color: '#3B82F6' },
  published: { label: 'منشور', color: '#10B981' },
  draft:     { label: 'مسودة', color: '#9CA3AF' },
  failed:    { label: 'فشل',   color: '#EF4444' },
};

@Component({
  selector: 'app-campaign-post-detail-page',
  imports: [PageHeader, RouterLink, TooltipDirective],
  templateUrl: './campaign-post-detail-page.html',
  styleUrls: ['../../dashboard/dashboard-shared.css', './campaign-post-detail-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CampaignPostDetailPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly campaignService = inject(CampaignService);
  private readonly scheduledPostService = inject(ScheduledPostService);
  private readonly analyticsService = inject(AnalyticsService);
  private readonly coinPricingService = inject(CoinPricingService);
  protected readonly perms = inject(PermissionService);
  private readonly seo = inject(SeoService);
  private readonly errorModalService = inject(ErrorModalService);
  private readonly confirmDialogService = inject(ConfirmDialogService);

  /// Reactive route params — the router reuses this component instance across navigations that
  /// only change `:id`/`:postId`, so a snapshot read here would freeze on the first post forever.
  private readonly paramMap = toSignal(this.route.paramMap, { requireSync: true });
  protected readonly campaignId = computed(() => this.paramMap().get('id') ?? '');
  protected readonly postId = computed(() => this.paramMap().get('postId') ?? '');

  /** Fetched, not read out of CampaignService's list — the list is loaded once by UserLayout, so
   *  a deep link to a post rendered "الحملة غير موجودة" for a campaign that exists. It also gates
   *  the scheduled-post fetch below, which is what actually resolves `post()`. */
  protected readonly campaign = signal<GetCampaignResponse | null>(null);
  protected readonly post = computed(() => this.scheduledPostService.getById(this.postId())());

  /** Distinguishes "still fetching the campaign and its posts" from "this post really doesn't
   *  exist" — the page used to render its not-found state during the load of every deep link. */
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);

  protected readonly platformMeta = CAMPAIGN_PLATFORM_META;
  protected readonly statusMeta = STATUS_META;

  protected readonly snapshots = signal<PostAnalyticsSnapshot[]>([]);
  protected readonly analyticsUnsupportedNote = signal<string | null>(null);
  /** Distinguishes "still fetching analytics" (spinner) from "fetch finished with zero snapshots"
   *  (empty state) — without this the empty-state copy flashed for a moment on every load. */
  protected readonly analyticsLoading = signal(true);
  /** A genuine failure (network/5xx) fetching analytics — kept separate from "no snapshots yet",
   *  which is a normal 200 with an empty list, not an error. */
  protected readonly analyticsError = signal<string | null>(null);
  protected readonly syncing = signal(false);
  protected readonly publishing = signal(false);

  protected readonly reschedulingOpen = signal(false);
  protected readonly rescheduleDate = signal('');
  protected readonly rescheduleTime = signal('');
  protected readonly rescheduling = signal(false);

  protected readonly latestSnapshot = computed(() => this.snapshots()[0] ?? null);
  /** The snapshot immediately before the latest one — the "prior" side of Growth/Change-over-time
   *  (Phase 11). Sourced from the same history array `latestSnapshot` already reads; no new fetch. */
  protected readonly priorSnapshot = computed(() => this.snapshots()[1] ?? null);

  /** Phase 11 — Growth/Change-over-time: delta between the two most recent snapshots per metric.
   *  Null (not zero) whenever there isn't yet a prior snapshot to compare against. */
  protected readonly growth = computed<Record<'views' | 'uniqueViewers' | 'likes' | 'comments' | 'shares' | 'engagementRate', MetricGrowth> | null>(() => {
    const latest = this.latestSnapshot();
    const prior = this.priorSnapshot();
    if (!latest || !prior) return null;
    return {
      views: computeMetricGrowth(latest.views, prior.views),
      uniqueViewers: computeMetricGrowth(latest.uniqueViewers, prior.uniqueViewers),
      likes: computeMetricGrowth(latest.likes, prior.likes),
      comments: computeMetricGrowth(latest.comments, prior.comments),
      shares: computeMetricGrowth(latest.shares, prior.shares),
      engagementRate: computeMetricGrowth(latest.engagementRate, prior.engagementRate),
    };
  });

  /** Phase 11 — Post vs. Campaign Average: fetched once per campaign, alongside the post's own
   *  analytics, so the comparison below can consume the backend's already-computed campaign-level
   *  weighted Engagement Rate instead of re-deriving it. */
  protected readonly campaignAnalytics = signal<CampaignAnalyticsSummary | null>(null);
  protected readonly campaignAnalyticsLoading = signal(true);
  protected readonly postVsCampaignAverage = computed<PostVsCampaignAverage | null>(() => {
    const campaign = this.campaignAnalytics();
    if (!campaign) return null;
    return comparePostToCampaignAverage(this.latestSnapshot()?.engagementRate ?? null, campaign.averageEngagementRate);
  });
  /** Views/UniqueViewers/EngagementRate/Clicks require extended platform permissions that may not
   *  be granted yet, so they're gated behind an availability flag. Likes/Comments/Shares are core
   *  metrics Meta always returns for a *successful* fetch, but the single Graph API call that
   *  fetches all three together can still fail on its own (expired token, transient error) while
   *  the separate Insights call succeeds — gating these too means that failure renders as "—"
   *  instead of an indistinguishable, misleading 0. */
  protected readonly viewsAvailable = computed(() => metricAvailable(this.snapshots(), 'views'));
  protected readonly uniqueViewersAvailable = computed(() => metricAvailable(this.snapshots(), 'uniqueViewers'));
  protected readonly engagementRateAvailable = computed(() => metricAvailable(this.snapshots(), 'engagementRate'));
  protected readonly clicksAvailable = computed(() => metricAvailable(this.snapshots(), 'clicks'));
  protected readonly likesAvailable = computed(() => metricAvailable(this.snapshots(), 'likes'));
  protected readonly commentsAvailable = computed(() => metricAvailable(this.snapshots(), 'comments'));
  protected readonly sharesAvailable = computed(() => metricAvailable(this.snapshots(), 'shares'));

  constructor() {
    effect(() => {
      const id = this.campaignId();
      const postId = this.postId();
      this.seo.setPageSeo({
        title: 'تفاصيل المنشور | رواج',
        description: 'بيانات المنشور المجدول: المنصة، المحتوى، والموعد.',
        keywords: 'رواج, تفاصيل المنشور, جدولة المنشورات',
        path: '/dashboard/campaigns/' + id + '/posts/' + postId,
        image: '/home-hero-light.png',
        type: 'website',
        noIndex: true,
      });
    });

    // ScheduledPostService's list is otherwise only ever populated by /dashboard/calendar —
    // without this, opening a post by direct link or reloading this page always renders
    // "المنشور غير موجود" even for a post that exists.
    effect(() => {
      const id = this.campaignId();
      untracked(() => this.loadCampaignAndPosts(id));
    });

    // Re-fetches analytics whenever the route's post id actually changes — the router reuses
    // this component instance across in-app navigation between two posts' detail pages.
    effect(() => {
      const postId = this.postId();
      this.snapshots.set([]);
      this.analyticsUnsupportedNote.set(null);
      this.analyticsError.set(null);
      if (postId) this.loadAnalytics(postId);
      else this.analyticsLoading.set(false);
    });

    // Powers Post vs. Campaign Average (Phase 11) — reuses the same campaign analytics endpoint
    // Phase 8's Campaign Dashboard already calls, keyed on campaignId (not postId) since it's the
    // same fetch regardless of which of the campaign's posts is open.
    effect(() => {
      const id = this.campaignId();
      this.campaignAnalytics.set(null);
      if (id) this.loadCampaignAnalytics(id);
      else this.campaignAnalyticsLoading.set(false);
    });
  }

  /** Loads the campaign, then its scheduled posts — `post()` resolves out of the latter, so the
   *  page stays in its loading state until both have settled. */
  private loadCampaignAndPosts(campaignId: string): void {
    this.campaign.set(null);
    this.loadError.set(null);
    if (!campaignId) {
      this.loading.set(false);
      this.loadError.set('لم يتم العثور على الحملة.');
      return;
    }

    this.loading.set(true);
    this.campaignService.getCampaign(campaignId).subscribe({
      next: res => {
        if (!res.data) {
          this.loading.set(false);
          this.loadError.set('لم يتم العثور على الحملة.');
          return;
        }
        this.campaign.set(res.data);
        this.scheduledPostService.refresh(res.data.brandProfileId, campaignId).subscribe({
          next: () => this.loading.set(false),
          error: () => this.loading.set(false),
        });
      },
      error: err => {
        this.loading.set(false);
        this.loadError.set(extractApiErrorMessage(err, 'تعذّر تحميل بيانات الحملة.'));
      },
    });
  }

  private loadAnalytics(postId: string): void {
    this.analyticsLoading.set(true);
    this.analyticsError.set(null);
    this.analyticsService.getPost(postId).subscribe({
      next: res => {
        this.analyticsLoading.set(false);
        // Success with an empty list is the normal "no snapshots synced yet" state, not an error.
        if (res.data) this.snapshots.set(res.data);
      },
      error: err => {
        this.analyticsLoading.set(false);
        this.analyticsError.set(extractApiErrorMessage(err, 'تعذّر تحميل إحصاءات المنشور.'));
      },
    });
  }

  /** Engagements = Likes + Comments + Shares, per the approved KPI definitions — never a metric the
   *  backend returns directly, always summed from the raw counts already on the snapshot. */
  protected engagementsOf(snap: PostAnalyticsSnapshot): number {
    return (snap.likes ?? 0) + (snap.comments ?? 0) + (snap.shares ?? 0);
  }

  /** Phase 11 — Post vs. Campaign Average: silently leaves `campaignAnalytics` null on failure
   *  (the comparison widget simply doesn't render, same as "no data yet") rather than surfacing a
   *  second error banner alongside the post analytics one above. */
  private loadCampaignAnalytics(campaignId: string): void {
    this.campaignAnalyticsLoading.set(true);
    this.analyticsService.getCampaign(campaignId).subscribe({
      next: res => {
        this.campaignAnalyticsLoading.set(false);
        if (res.data) this.campaignAnalytics.set(res.data);
      },
      error: () => this.campaignAnalyticsLoading.set(false),
    });
  }

  protected retryAnalytics(): void {
    const postId = this.postId();
    if (postId) this.loadAnalytics(postId);
  }

  protected readonly formatEngagementRate = formatEngagementRate;

  protected formatDateTime(iso: string): string {
    return new Date(iso).toLocaleDateString('ar-EG', {
      weekday: 'long', day: 'numeric', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit', hour12: true,
    });
  }

  protected formatShortDateTime(iso: string): string {
    return new Date(iso).toLocaleDateString('ar-EG', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit', hour12: true });
  }

  protected syncAnalytics(): void {
    if (this.syncing() || !this.perms.canEdit()) return;
    this.syncing.set(true);
    this.analyticsUnsupportedNote.set(null);
    const postId = this.postId();
    this.analyticsService.syncPost(postId).subscribe({
      next: () => {
        this.syncing.set(false);
        this.loadAnalytics(postId);
      },
      error: err => {
        this.syncing.set(false);
        const message = extractApiErrorMessage(err, 'تعذّرت مزامنة الإحصاءات.');
        // "Analytics are not yet supported for Instagram" etc. is an informational platform
        // limitation, not a failure the user did anything wrong to cause — show it inline.
        if (message.toLowerCase().includes('not yet supported')) {
          this.analyticsUnsupportedNote.set('إحصاءات هذه المنصة غير متاحة بعد.');
        } else {
          this.errorModalService.show(message, { variant: 'error' });
        }
      },
    });
  }

  protected publishNow(): void {
    if (this.publishing() || !this.perms.canEdit()) return;
    this.publishing.set(true);
    this.scheduledPostService.publishNow(this.postId()).subscribe({
      next: () => {
        this.publishing.set(false);
        this.coinPricingService.refreshAfterSpend();
      },
      error: err => {
        this.publishing.set(false);
        this.coinPricingService.refreshAfterSpend();
        this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر نشر المنشور الآن.'), { variant: 'error' });
      },
    });
  }

  protected startReschedule(): void {
    if (!this.perms.canEdit()) return;
    const p = this.post();
    if (p) {
      this.rescheduleDate.set(p.scheduledAt.substring(0, 10));
      this.rescheduleTime.set(p.scheduledAt.substring(11, 16));
    }
    this.reschedulingOpen.set(true);
  }

  protected cancelReschedule(): void {
    this.reschedulingOpen.set(false);
  }

  protected updateRescheduleDate(value: string): void { this.rescheduleDate.set(value); }
  protected updateRescheduleTime(value: string): void { this.rescheduleTime.set(value); }

  protected submitReschedule(): void {
    if (this.rescheduling() || !this.rescheduleDate() || !this.rescheduleTime()) return;
    this.rescheduling.set(true);
    const iso = `${this.rescheduleDate()}T${this.rescheduleTime()}:00`;
    this.scheduledPostService.reschedule(this.postId(), iso).subscribe({
      next: () => {
        this.rescheduling.set(false);
        this.reschedulingOpen.set(false);
        this.coinPricingService.refreshAfterSpend();
      },
      error: err => {
        this.rescheduling.set(false);
        this.coinPricingService.refreshAfterSpend();
        this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر تحديث موعد النشر.'), { variant: 'error' });
      },
    });
  }

  protected async deletePost(): Promise<void> {
    if (!this.perms.canEdit()) return;
    const confirmed = await this.confirmDialogService.confirm(
      'سيتم إلغاء جدولة هذا المنشور نهائيًا ولن يُنشر. هل أنت متأكد؟',
      { title: 'حذف المنشور', confirmLabel: 'حذف', variant: 'danger' },
    );
    if (!confirmed) return;

    const postId = this.postId();
    this.scheduledPostService.cancel(postId).subscribe({
      next: () => {
        this.scheduledPostService.remove(postId);
        this.coinPricingService.refreshAfterSpend();
        this.router.navigate(['/dashboard/campaigns', this.campaignId(), 'calendar']);
      },
      error: err => this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر إلغاء جدولة المنشور.'), { variant: 'error' }),
    });
  }
}
