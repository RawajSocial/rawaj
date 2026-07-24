import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { CampaignService } from '../../../services/campaign.service';
import { ScheduledPostService } from '../../../services/scheduled-post.service';
import { AnalyticsService } from '../../../services/analytics.service';
import { CoinPricingService } from '../../../services/coin-pricing.service';
import { PermissionService } from '../../../core/tenant/permission.service';
import { TooltipDirective } from '../../../shared/directives/tooltip.directive';
import { CampaignPlatform } from '../../../model/campaign.model';
import { PostStatus } from '../../../model/scheduled-post.model';
import { PostAnalyticsSnapshot, metricAvailable } from '../../../model/analytics.model';
import { SeoService } from '../../../services/seo.service';
import { ErrorModalService } from '../../../services/error-modal.service';
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

  /// Reactive route params — the router reuses this component instance across navigations that
  /// only change `:id`/`:postId`, so a snapshot read here would freeze on the first post forever.
  private readonly paramMap = toSignal(this.route.paramMap, { requireSync: true });
  protected readonly campaignId = computed(() => this.paramMap().get('id') ?? '');
  protected readonly postId = computed(() => this.paramMap().get('postId') ?? '');

  protected readonly campaign = computed(() => this.campaignService.getById(this.campaignId())());
  protected readonly post = computed(() => this.scheduledPostService.getById(this.postId())());

  protected readonly platformMeta = PLATFORM_META;
  protected readonly statusMeta = STATUS_META;

  protected readonly snapshots = signal<PostAnalyticsSnapshot[]>([]);
  protected readonly analyticsUnsupportedNote = signal<string | null>(null);
  protected readonly syncing = signal(false);
  protected readonly publishing = signal(false);

  protected readonly reschedulingOpen = signal(false);
  protected readonly rescheduleDate = signal('');
  protected readonly rescheduleTime = signal('');
  protected readonly rescheduling = signal(false);

  protected readonly latestSnapshot = computed(() => this.snapshots()[0] ?? null);
  protected readonly reachAvailable = computed(() => metricAvailable(this.snapshots(), 'reach'));

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
      const brandProfileId = this.campaign()?.brandProfileId;
      const id = this.campaignId();
      if (brandProfileId) this.scheduledPostService.refresh(brandProfileId, id).subscribe();
    });

    // Re-fetches analytics whenever the route's post id actually changes — the router reuses
    // this component instance across in-app navigation between two posts' detail pages.
    effect(() => {
      const postId = this.postId();
      this.snapshots.set([]);
      this.analyticsUnsupportedNote.set(null);
      if (postId) this.loadAnalytics(postId);
    });
  }

  private loadAnalytics(postId: string): void {
    this.analyticsService.getPost(postId).subscribe({
      next: res => { if (res.data) this.snapshots.set(res.data); },
      error: () => { /* no analytics yet is a normal state, not an error to surface */ },
    });
  }

  protected formatDateTime(iso: string): string {
    return new Date(iso).toLocaleDateString('ar-SA', {
      weekday: 'long', day: 'numeric', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit', hour12: true,
    });
  }

  protected formatShortDateTime(iso: string): string {
    return new Date(iso).toLocaleDateString('ar-SA', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit', hour12: true });
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

  protected deletePost(): void {
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
