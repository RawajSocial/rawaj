import { Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { CampaignService } from '../../../services/campaign.service';
import { ContentItemService } from '../../../services/content-item.service';
import { ScheduledPostService } from '../../../services/scheduled-post.service';
import { CoinPricingService } from '../../../services/coin-pricing.service';
import { SocialAccountService } from '../../../core/social/social-account.service';
import { PermissionService } from '../../../core/tenant/permission.service';
import { SeoService } from '../../../services/seo.service';
import { ErrorModalService } from '../../../services/error-modal.service';
import { extractApiErrorMessage } from '../../../core/auth/api-error.util';
import { parseInsufficientCoins } from '../../../core/auth/coin-error.util';
import { CoinCostHint } from '../../../shared/components/coin-cost-hint/coin-cost-hint';
import { TooltipDirective } from '../../../shared/directives/tooltip.directive';
import { GetCampaignResponse, ScheduleCampaignPostResult } from '../../../model/campaign.model';
import { ContentItemSummary } from '../../../model/content-item.model';
import { SocialAccountSummary } from '../../../model/social-account.model';

const CONTENT_TYPE_LABELS: Record<ContentItemSummary['contentType'], string> = {
  Post: 'بوست', Story: 'قصة', ReelScript: 'ريل', AdCopy: 'إعلان', Blog: 'مقال', Caption: 'كابشن',
};

const PLATFORM_LABELS: Record<ContentItemSummary['platform'], string> = {
  Instagram: 'إنستغرام', Facebook: 'فيسبوك', Tiktok: 'تيك توك', Twitter: 'إكس', Youtube: 'يوتيوب', Linkedin: 'لينكدإن',
};

const STATUS_LABELS: Record<ContentItemSummary['status'], string> = {
  Draft: 'مسودة', Reviewed: 'تمت المراجعة', Approved: 'مقبول', Rejected: 'مرفوض', Published: 'منشور',
};

interface PlatformConnectionStatus {
  platform: ContentItemSummary['platform'];
  connected: boolean;
  accountName?: string;
}

@Component({
  selector: 'app-campaign-content-page',
  imports: [PageHeader, CoinCostHint, TooltipDirective],
  templateUrl: './campaign-content-page.html',
  styleUrls: ['../../dashboard/dashboard-shared.css', './campaign-content-page.css'],
})
export class CampaignContentPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly campaignService = inject(CampaignService);
  private readonly contentItemService = inject(ContentItemService);
  private readonly scheduledPostService = inject(ScheduledPostService);
  protected readonly coinPricingService = inject(CoinPricingService);
  private readonly socialAccountService = inject(SocialAccountService);
  protected readonly perms = inject(PermissionService);
  private readonly errorModalService = inject(ErrorModalService);
  private readonly seo = inject(SeoService);

  protected readonly contentTypeLabels = CONTENT_TYPE_LABELS;
  protected readonly platformLabels = PLATFORM_LABELS;
  protected readonly statusLabels = STATUS_LABELS;
  protected readonly pricing = this.coinPricingService.pricing;

  /// Reactive route param — the router reuses this component instance across navigations that
  /// only change `:id`, so a snapshot read here would freeze on the first campaign forever.
  private readonly paramMap = toSignal(this.route.paramMap, { requireSync: true });
  protected readonly campaignId = computed(() => this.paramMap().get('id') ?? '');
  protected readonly campaign = signal<GetCampaignResponse | null>(null);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);

  protected readonly items = this.contentItemService.items;
  protected readonly generating = signal(false);
  protected readonly postCount = signal(6);

  protected readonly socialAccounts = signal<SocialAccountSummary[]>([]);

  /** Connection status per distinct platform present in the generated content — shown to
   *  everyone (read-only info), independent of role. */
  protected readonly platformStatuses = computed<PlatformConnectionStatus[]>(() => {
    const platforms = [...new Set(this.items().map(i => i.platform))];
    const accounts = this.socialAccounts();
    return platforms.map(platform => {
      const account = accounts.find(a => a.platform === platform && a.isActive);
      return { platform, connected: !!account, accountName: account?.accountName };
    });
  });

  protected readonly hasAnyConnectedAccount = computed(() => this.socialAccounts().some(a => a.isActive));
  protected readonly missingPlatformLabels = computed(() =>
    this.platformStatuses().filter(s => !s.connected).map(s => this.platformLabels[s.platform]),
  );

  protected readonly scheduling = signal(false);
  protected readonly scheduleResult = signal<{
    succeeded: number; failed: number; skipped: number; results: ScheduleCampaignPostResult[];
  } | null>(null);

  /** Per-item feedback textarea, keyed by contentItemId — only one open at a time. */
  protected readonly regeneratingId = signal<string | null>(null);
  protected readonly regenerateFeedback = signal('');
  protected readonly busyItemId = signal<string | null>(null);

  /** Content items with an active (Pending or Published) scheduled post — the backend deliberately
   *  leaves ContentItem.Status as Approved after scheduling (dedup happens server-side against
   *  ScheduledPosts, not content status), so without this "approved" would keep including posts
   *  that are already scheduled, overstating the coin-cost hint and, once nothing new is left to
   *  schedule, surfacing a generic-looking error for what's actually a no-op. Failed/cancelled
   *  posts are NOT active, since the backend allows re-scheduling those. */
  private readonly activelyScheduledContentItemIds = computed(() => new Set(
    this.scheduledPostService.byCampaign(this.campaignId())()
      .filter(p => p.status === 'scheduled' || p.status === 'published')
      .map(p => p.contentItemId),
  ));

  protected readonly approvedItems = computed(() =>
    this.items().filter(i => i.status === 'Approved' && !this.activelyScheduledContentItemIds().has(i.contentItemId)),
  );

  /** Guards the one-shot auto-generation triggered by ?autogenerate=1 (see rawaj-onboarding's
   *  approvePlan()) so a reload of this page never re-fires a 1000-coin generation. */
  private autogenerateRequested = false;

  constructor() {
    this.coinPricingService.ensureLoaded();
    this.autogenerateRequested = this.route.snapshot.queryParamMap.get('autogenerate') === '1';

    effect(() => {
      const id = this.campaignId();
      this.seo.setPageSeo({
        title: 'محتوى الحملة | رواج',
        description: 'راجع منشورات الحملة، اقبلها أو ارفضها أو أعد توليدها، ثم جدولها.',
        keywords: 'رواج, محتوى الحملة, مراجعة المحتوى, جدولة',
        path: '/dashboard/campaigns/' + id + '/content',
        image: '/home-hero-light.png',
        type: 'website',
        noIndex: true,
      });
    });

    // Re-loads everything whenever the route's campaign id actually changes — the router reuses
    // this component instance across in-app navigation between two campaigns' content pages.
    effect(() => {
      const id = this.campaignId();
      if (id) this.load(id);
    });
  }

  private load(campaignId: string): void {
    this.loading.set(true);
    this.campaignService.getCampaign(campaignId).subscribe({
      next: res => {
        this.loading.set(false);
        if (!res.data) {
          this.loadError.set('لم يتم العثور على الحملة.');
          return;
        }
        this.campaign.set(res.data);
        this.contentItemService.refresh(res.data.brandProfileId, campaignId).subscribe(() => {
          this.maybeAutogenerate(res.data!);
        });
        this.scheduledPostService.refresh(res.data.brandProfileId, campaignId).subscribe();
        this.socialAccountService.getByBrand(res.data.brandProfileId).subscribe(r => {
          if (r.data) this.socialAccounts.set(r.data);
        });
      },
      error: err => {
        this.loading.set(false);
        this.loadError.set(extractApiErrorMessage(err, 'تعذّر تحميل بيانات الحملة.'));
      },
    });
  }

  /** Fires content generation automatically once, right after the onboarding wizard's approval
   *  step — "if he accepts it we should generate the content for him". Only when there's nothing
   *  generated yet and the user is actually allowed to generate (an Editor might approve then hand
   *  off to a Viewer opening the same link). Strips the query param either way so a reload never
   *  re-triggers it. */
  private maybeAutogenerate(campaign: GetCampaignResponse): void {
    const shouldAutogenerate = this.autogenerateRequested
      && !!campaign.planApprovedAt
      && this.items().length === 0
      && this.perms.canEdit();
    this.autogenerateRequested = false;
    void this.router.navigate([], { relativeTo: this.route, queryParams: {}, replaceUrl: true });

    if (shouldAutogenerate) this.generateContent();
  }

  protected updatePostCount(value: string): void {
    const n = parseInt(value, 10);
    this.postCount.set(isNaN(n) ? 1 : Math.max(1, Math.min(n, 20)));
  }

  protected generateContent(): void {
    const campaign = this.campaign();
    if (!campaign || this.generating() || !this.perms.canEdit()) return;

    if (!campaign.planApprovedAt) {
      this.errorModalService.show(
        'يجب اعتماد استراتيجية الحملة أولاً قبل توليد المحتوى.', { variant: 'warning' },
      );
      return;
    }

    this.generating.set(true);
    this.campaignService.generateContent(this.campaignId(), {
      postCount: this.postCount(),
      language: 'Ar',
      includeImages: true,
    }).subscribe({
      next: () => {
        this.generating.set(false);
        this.coinPricingService.refreshAfterSpend();
        this.contentItemService.refresh(campaign.brandProfileId, this.campaignId()).subscribe();
      },
      error: err => {
        this.generating.set(false);
        this.coinPricingService.refreshAfterSpend();
        this.showSpendError(err, 'تعذّر توليد المحتوى.');
      },
    });
  }

  protected review(item: ContentItemSummary, approve: boolean): void {
    if (this.busyItemId() || !this.perms.canEdit()) return;
    this.busyItemId.set(item.contentItemId);
    this.contentItemService.review(item.contentItemId, approve).subscribe({
      next: () => {
        this.busyItemId.set(null);
        const brandProfileId = this.campaign()?.brandProfileId;
        if (brandProfileId) this.contentItemService.refresh(brandProfileId, this.campaignId()).subscribe();
      },
      error: err => {
        this.busyItemId.set(null);
        this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر تحديث حالة المنشور.'), { variant: 'error' });
      },
    });
  }

  protected startRegenerate(item: ContentItemSummary): void {
    if (!this.perms.canEdit()) return;
    this.regeneratingId.set(item.contentItemId);
    this.regenerateFeedback.set('');
  }

  protected cancelRegenerate(): void {
    this.regeneratingId.set(null);
    this.regenerateFeedback.set('');
  }

  protected updateRegenerateFeedback(value: string): void {
    this.regenerateFeedback.set(value);
  }

  protected submitRegenerate(item: ContentItemSummary): void {
    const feedback = this.regenerateFeedback().trim();
    if (!feedback || this.busyItemId() || !this.perms.canEdit()) return;

    this.busyItemId.set(item.contentItemId);
    this.contentItemService.regenerate(item.contentItemId, feedback).subscribe({
      next: () => {
        this.busyItemId.set(null);
        this.regeneratingId.set(null);
        this.regenerateFeedback.set('');
        this.coinPricingService.refreshAfterSpend();
        const brandProfileId = this.campaign()?.brandProfileId;
        if (brandProfileId) this.contentItemService.refresh(brandProfileId, this.campaignId()).subscribe();
      },
      error: err => {
        this.busyItemId.set(null);
        this.coinPricingService.refreshAfterSpend();
        this.showSpendError(err, 'تعذّر إعادة توليد المنشور.');
      },
    });
  }

  protected schedulePosts(): void {
    if (this.scheduling() || this.approvedItems().length === 0 || !this.perms.canEdit()) return;

    this.scheduling.set(true);
    this.scheduleResult.set(null);
    this.campaignService.schedulePosts(this.campaignId()).subscribe({
      next: res => {
        this.scheduling.set(false);
        this.coinPricingService.refreshAfterSpend();
        if (res.data) {
          this.scheduleResult.set({
            succeeded: res.data.succeededCount,
            failed: res.data.failedCount,
            skipped: res.data.skippedCount,
            results: res.data.results,
          });
          const brandProfileId = this.campaign()?.brandProfileId;
          if (brandProfileId) {
            this.contentItemService.refresh(brandProfileId, this.campaignId()).subscribe();
            this.scheduledPostService.refresh(brandProfileId, this.campaignId()).subscribe();
          }
        }
      },
      error: err => {
        this.scheduling.set(false);
        this.coinPricingService.refreshAfterSpend();
        this.showSpendError(err, 'تعذّر جدولة منشورات الحملة.');
      },
    });
  }

  protected goToConnectSocialAccounts(): void {
    void this.router.navigate(['/dashboard/social-accounts']);
  }

  /** Shows a coin-aware Arabic message with a "شحن الرصيد" CTA when the failure is a shortfall,
   *  otherwise falls back to the normal error modal. Used by every coin-spending action here. */
  private showSpendError(err: unknown, fallback: string): void {
    const shortfall = parseInsufficientCoins(err);
    if (shortfall) {
      this.errorModalService.show(
        `تحتاج ${shortfall.required.toLocaleString('ar-SA')} كوين لإتمام هذا الإجراء، ورصيدك الحالي ${shortfall.balance.toLocaleString('ar-SA')} كوين.`,
        { variant: 'warning', actionLabel: 'شحن الرصيد', actionLink: ['/dashboard/billing'] },
      );
      return;
    }
    this.errorModalService.show(extractApiErrorMessage(err, fallback), { variant: 'error' });
  }
}
