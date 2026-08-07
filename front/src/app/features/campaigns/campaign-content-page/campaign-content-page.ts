import { Component, DestroyRef, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, forkJoin, map, of } from 'rxjs';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { CampaignService } from '../../../services/campaign.service';
import { ContentItemService } from '../../../services/content-item.service';
import { ScheduledPostService } from '../../../services/scheduled-post.service';
import { VisualAssetService } from '../../../services/visual-asset.service';
import { CoinPricingService } from '../../../services/coin-pricing.service';
import { AiPipelineService } from '../../../services/ai-pipeline.service';
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

/** Quick-start prompts for the regenerate box — the exact tone shifts the workflow spec calls
 *  out ("more professional", "humorous", "younger audience") as one-tap chips instead of the
 *  user having to type them out for every post they want tweaked the same way. */
const REGENERATE_PRESETS = ['اجعله أكثر احترافية', 'استخدم نبرة أكثر فكاهية', 'استهدف جمهورًا أصغر سنًا'];

/** Mirrors GenerateCampaignContentCommandValidator's `InclusiveBetween(1, 15)` on the backend. */
const MIN_POST_COUNT = 1;
const MAX_POST_COUNT = 15;

interface PlatformConnectionStatus {
  platform: ContentItemSummary['platform'];
  connected: boolean;
  accountName?: string;
}

@Component({
  selector: 'app-campaign-content-page',
  imports: [PageHeader, CoinCostHint, TooltipDirective, RouterLink],
  templateUrl: './campaign-content-page.html',
  styleUrls: ['../../dashboard/dashboard-shared.css', './campaign-content-page.css'],
})
export class CampaignContentPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly campaignService = inject(CampaignService);
  private readonly contentItemService = inject(ContentItemService);
  private readonly scheduledPostService = inject(ScheduledPostService);
  private readonly visualAssetService = inject(VisualAssetService);
  protected readonly coinPricingService = inject(CoinPricingService);
  protected readonly aiPipelineService = inject(AiPipelineService);
  private readonly socialAccountService = inject(SocialAccountService);
  protected readonly perms = inject(PermissionService);
  private readonly errorModalService = inject(ErrorModalService);
  private readonly seo = inject(SeoService);

  protected readonly contentTypeLabels = CONTENT_TYPE_LABELS;
  protected readonly platformLabels = PLATFORM_LABELS;
  protected readonly statusLabels = STATUS_LABELS;
  protected readonly pricing = this.coinPricingService.pricing;
  protected readonly regeneratePresets = REGENERATE_PRESETS;

  /// Reactive route param — the router reuses this component instance across navigations that
  /// only change `:id`, so a snapshot read here would freeze on the first campaign forever.
  private readonly paramMap = toSignal(this.route.paramMap, { requireSync: true });
  protected readonly campaignId = computed(() => this.paramMap().get('id') ?? '');
  protected readonly campaign = signal<GetCampaignResponse | null>(null);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);

  protected readonly items = this.contentItemService.items;
  protected readonly postCount = signal(6);
  protected readonly minPostCount = MIN_POST_COUNT;
  protected readonly maxPostCount = MAX_POST_COUNT;

  /** Set the moment "توليد المحتوى" is clicked, cleared once that call's own response lands —
   *  gives the button an instant disabled state on click, before the first poll tick could
   *  possibly reflect it. */
  private readonly requestInFlight = signal(false);

  /** True while the campaign's pipeline run actually has a `ContentPlan`/`ContentImage` stage
   *  outstanding (`Pending` or `Running`) — real, server-derived state, not a local guess. This is
   *  what closes the reload double-spend gap: a reload picks this up from the very first poll tick
   *  (started in `load()` whenever the campaign has a `currentPipelineRunId`), so a batch already
   *  running from a previous tab/session disables the button here too, not just in the tab that
   *  started it. */
  protected readonly contentBatchInFlight = computed(() => {
    const run = this.aiPipelineService.run();
    if (!run) return false;
    return run.stages.some(s =>
      (s.kind === 'ContentPlan' || s.kind === 'ContentImage') && (s.status === 'Pending' || s.status === 'Running'));
  });

  protected readonly generating = computed(() => this.requestInFlight() || this.contentBatchInFlight());

  /** Informational only — the backend worker retries a coins-blocked stage on its own poll cycle
   *  once a top-up lands, so there is nothing to click here (same as the strategy page's note). */
  protected readonly awaitingCoins = computed(() => this.aiPipelineService.run()?.status === 'AwaitingCoins');

  /** How many posts are actually ready to render as real cards — has its image, whether generated
   *  or the exhausted-retries placeholder every post ends up with eventually, same `!!imageUrl`
   *  gate `approvedItems` below already uses. A post whose text exists but whose image hasn't
   *  landed yet stays a skeleton rather than a text-only card, so this page keeps the two visual
   *  states it already had instead of inventing a third. */
  protected readonly readyItems = computed(() => this.items().filter(i => !!i.imageUrl));

  /** Real per-image progress from the polled run (`AiPipelineProgressPolicy`, server-computed) —
   *  zero before ContentPlan's fan-out exists, i.e. before individual post identities even exist. */
  private readonly imagesTotal = computed(() => this.aiPipelineService.run()?.progress.imagesTotal ?? 0);
  private readonly imagesCompleted = computed(() => this.aiPipelineService.run()?.progress.imagesCompleted ?? 0);

  /** Only the posts still missing, not the whole requested batch — this used to be `postCount()`'s
   *  CURRENT field value (wrong after a reload, or if that field changed) and never shrank as
   *  individual posts finished, so every slot stayed a skeleton until the entire batch completed
   *  and the page was reloaded. Falls back to the requested count before the fan-out exists, since
   *  real post identities aren't known yet at that point. */
  protected readonly generationSkeletonCards = computed(() => {
    const total = this.imagesTotal() || this.postCount();
    const remaining = Math.max(0, total - this.readyItems().length);
    return Array.from({ length: remaining }, (_, i) => i);
  });

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

  /** Per-item image retry — a post that came back from generation with no image (best-effort
   *  image generation can fail or run out of monthly AI credits mid-batch) gets a distinct
   *  "missing image" state and a one-click retry instead of silently being reviewable without one. */
  protected readonly retryingImageId = signal<string | null>(null);

  /** Per-item inline scheduling — one post at a time, alongside the bulk "schedule all approved"
   *  action below for when there's nothing item-specific to decide (account/time). */
  protected readonly schedulingItemId = signal<string | null>(null);
  protected readonly scheduleAccountId = signal<string | null>(null);
  protected readonly scheduleDate = signal('');
  protected readonly scheduleTime = signal('');
  protected readonly schedulingItemBusy = signal(false);

  /** The backend rejects anything less than 10 minutes out (native platform scheduling needs the
   *  lead time) — checked client-side too so a too-soon pick is caught here, with a plain-language
   *  reason, instead of surfacing the backend's generic "Validation failed." with no detail (the
   *  field-level FluentValidation message never reaches `extractApiErrorMessage`, which only reads
   *  the top-level `message`). Shared by both the per-item and multi-select schedule panels, which
   *  reuse the same `scheduleDate`/`scheduleTime` fields. */
  protected readonly scheduleTimeTooSoon = computed(() => {
    const date = this.scheduleDate();
    const time = this.scheduleTime();
    if (!date || !time) return false;
    const picked = new Date(`${date}T${time}:00`);
    if (isNaN(picked.getTime())) return false;
    return picked.getTime() < Date.now() + 10 * 60 * 1000;
  });

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

  /** Approved AND has an image — a post is never eligible for scheduling (bulk or per-item)
   *  without one, enforcing "no post should exist without an image" at the gate that actually
   *  matters instead of at generation time, which can't be guaranteed server-side. */
  protected readonly approvedItems = computed(() =>
    this.items().filter(i => i.status === 'Approved' && !!i.imageUrl && !this.activelyScheduledContentItemIds().has(i.contentItemId)),
  );

  protected readonly approvedMissingImageCount = computed(() =>
    this.items().filter(i => i.status === 'Approved' && !i.imageUrl && !this.activelyScheduledContentItemIds().has(i.contentItemId)).length,
  );

  /** Multi-select scheduling — "schedule multiple posts" as its own path distinct from the
   *  per-item panel (exactly one) and the bulk "schedule all approved" button (every eligible
   *  post, no choice). Selection is only meaningful for posts that are actually schedulable. */
  protected readonly selectedItemIds = signal<Set<string>>(new Set());
  protected readonly selectedCount = computed(() => this.selectedItemIds().size);
  protected readonly selectedSchedulePanelOpen = signal(false);
  protected readonly bulkSelectedScheduling = signal(false);

  protected isSchedulable(item: ContentItemSummary): boolean {
    return item.status === 'Approved' && !!item.imageUrl && !this.activelyScheduledContentItemIds().has(item.contentItemId);
  }

  protected isSelected(item: ContentItemSummary): boolean {
    return this.selectedItemIds().has(item.contentItemId);
  }

  protected toggleSelectItem(item: ContentItemSummary): void {
    if (!this.isSchedulable(item)) return;
    this.selectedItemIds.update(set => {
      const next = new Set(set);
      if (next.has(item.contentItemId)) next.delete(item.contentItemId);
      else next.add(item.contentItemId);
      return next;
    });
  }

  protected clearSelection(): void {
    this.selectedItemIds.set(new Set());
    this.selectedSchedulePanelOpen.set(false);
  }

  /** Guards the one-shot auto-generation triggered by ?autogenerate=1 (see rawaj-onboarding's
   *  approvePlan()) so a reload of this page never re-fires a 1000-coin generation. */
  private autogenerateRequested = false;

  /** Last `imagesCompleted` value content items were actually re-fetched for — lets the effect
   *  below tell "a new image just finished" apart from "the same poll tick landed again", so it
   *  fetches once per real change instead of once per 2-second poll regardless of progress. */
  private lastRefreshedImageCount = -1;

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

    // The poll (AiPipelineService.startPolling) only ever updates run/stage STATUS — it never
    // re-fetches the content items themselves. Without this, a post's card never appeared until
    // the user reloaded the page: contentItemService.items() was fetched once in load() and once
    // right after generateContent()'s POST resolved, then never again — even though images keep
    // finishing for seconds or minutes afterward, one at a time, via the background worker.
    effect(() => {
      const run = this.aiPipelineService.run();
      if (!run) return;
      const completed = run.progress.imagesCompleted;
      if (completed === this.lastRefreshedImageCount) return;
      this.lastRefreshedImageCount = completed;

      const campaign = this.campaign();
      if (campaign) this.contentItemService.refresh(campaign.brandProfileId, this.campaignId()).subscribe();
    });

    this.destroyRef.onDestroy(() => this.aiPipelineService.clear());
  }

  private load(campaignId: string): void {
    this.resetCampaignScopedState();
    this.loading.set(true);
    this.campaignService.getCampaign(campaignId).subscribe({
      next: res => {
        this.loading.set(false);
        if (!res.data) {
          this.loadError.set('لم يتم العثور على الحملة.');
          return;
        }
        this.campaign.set(res.data);
        // Watches whatever run the campaign is already on, if any — a batch left running by a
        // previous tab/session shows up here on the very first poll tick, closing the reload gap.
        if (res.data.currentPipelineRunId) this.aiPipelineService.startPolling(res.data.currentPipelineRunId);
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

  /** Everything on this page belongs to one campaign, but the router reuses the component instance
   *  across campaigns and ContentItemService/ScheduledPostService hold a single global list. Without
   *  this, switching campaigns left the previous one's posts, selection, open regenerate/schedule
   *  panels, scheduling summary and load error on screen — the selection and the schedule panels
   *  being the dangerous ones, since acting on them would have scheduled another campaign's posts. */
  private resetCampaignScopedState(): void {
    this.loadError.set(null);
    this.contentItemService.clear();
    this.aiPipelineService.clear();
    this.socialAccounts.set([]);
    this.scheduleResult.set(null);
    this.selectedItemIds.set(new Set());
    this.selectedSchedulePanelOpen.set(false);
    this.regeneratingId.set(null);
    this.regenerateFeedback.set('');
    this.schedulingItemId.set(null);
    this.busyItemId.set(null);
    this.retryingImageId.set(null);
    this.lastRefreshedImageCount = -1;
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

  /** Clamped to the range GenerateCampaignContentCommandValidator actually accepts (1–15). The
   *  field used to allow up to 20, so asking for 16+ came back as the backend's bare
   *  "Validation failed." with no indication of which field or limit was wrong. */
  protected updatePostCount(value: string): void {
    const n = parseInt(value, 10);
    this.postCount.set(isNaN(n) ? 1 : Math.max(MIN_POST_COUNT, Math.min(n, MAX_POST_COUNT)));
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

    this.requestInFlight.set(true);

    this.campaignService.generateContent(this.campaignId(), {
      postCount: this.postCount(),
      language: 'Ar',
      includeImages: true,
    }).subscribe({
      next: () => {
        this.requestInFlight.set(false);
        this.coinPricingService.refreshAfterSpend();
        this.contentItemService.refresh(campaign.brandProfileId, this.campaignId()).subscribe();
      },
      error: err => {
        this.requestInFlight.set(false);
        this.coinPricingService.refreshAfterSpend();
        this.showSpendError(err, 'تعذّر توليد المحتوى.');
      },
    });
  }

  protected review(item: ContentItemSummary, approve: boolean): void {
    if (this.busyItemId() || !this.perms.canEdit()) return;
    if (approve && !item.imageUrl) return; // enforced in the template too — no accepting an imageless post
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

  /** Retries just the image for a post that came back from generation without one — reuses the
   *  standalone visual-asset generation endpoint, prompted with the post's own copy so the image
   *  matches what was actually written, same as the original per-item prompt server-side builds. */
  protected retryImage(item: ContentItemSummary): void {
    const campaign = this.campaign();
    if (!campaign || this.retryingImageId() || !this.perms.canEdit()) return;

    this.retryingImageId.set(item.contentItemId);
    this.visualAssetService.generate({
      brandProfileId: campaign.brandProfileId,
      campaignId: this.campaignId(),
      contentItemId: item.contentItemId,
      type: 'Image',
      prompt: item.content,
    }).subscribe({
      next: () => {
        this.retryingImageId.set(null);
        this.coinPricingService.refreshAfterSpend();
        this.contentItemService.refresh(campaign.brandProfileId, this.campaignId()).subscribe();
      },
      error: err => {
        this.retryingImageId.set(null);
        this.coinPricingService.refreshAfterSpend();
        this.showSpendError(err, 'تعذّر توليد الصورة.');
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

  /** Fills the feedback box from a quick preset ("more professional", etc.) instead of the user
   *  typing it out — appends rather than overwrites if they'd already started their own note. */
  protected applyRegeneratePreset(preset: string): void {
    const current = this.regenerateFeedback().trim();
    this.regenerateFeedback.set(current ? `${current}. ${preset}` : preset);
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

  /** Accounts connected for a given post's own platform — used both to default the per-item
   *  schedule panel's account picker and to let the user switch it when more than one exists. */
  protected accountsForPlatform(platform: ContentItemSummary['platform']): SocialAccountSummary[] {
    return this.socialAccounts().filter(a => a.platform === platform && a.isActive);
  }

  protected startSchedulePost(item: ContentItemSummary): void {
    if (!this.perms.canEdit()) return;
    const defaultAccount = this.accountsForPlatform(item.platform)[0];
    this.schedulingItemId.set(item.contentItemId);
    this.scheduleAccountId.set(defaultAccount?.socialAccountId ?? null);
    const soon = new Date(Date.now() + 60 * 60 * 1000); // an hour from now, a sensible default
    this.scheduleDate.set(soon.toISOString().slice(0, 10));
    this.scheduleTime.set(soon.toISOString().slice(11, 16));
  }

  protected cancelSchedulePost(): void {
    this.schedulingItemId.set(null);
  }

  protected updateScheduleAccount(value: string): void { this.scheduleAccountId.set(value); }
  protected updateScheduleDate(value: string): void { this.scheduleDate.set(value); }
  protected updateScheduleTime(value: string): void { this.scheduleTime.set(value); }

  /** Schedules exactly one post — the per-item counterpart to the bulk `schedulePosts()` below,
   *  for when the user wants to pick a specific account/time for a single post rather than
   *  accepting the bulk action's automatic per-platform routing. */
  protected submitSchedulePost(item: ContentItemSummary): void {
    const accountId = this.scheduleAccountId();
    if (!accountId || !this.scheduleDate() || !this.scheduleTime() || this.scheduleTimeTooSoon()
      || this.schedulingItemBusy() || !this.perms.canEdit()) return;

    this.schedulingItemBusy.set(true);
    const scheduledAt = `${this.scheduleDate()}T${this.scheduleTime()}:00`;
    this.scheduledPostService.schedule({
      contentItemId: item.contentItemId,
      visualAssetId: item.visualAssetId ?? undefined,
      socialAccountId: accountId,
      scheduledAt,
    }).subscribe({
      next: () => {
        this.schedulingItemBusy.set(false);
        this.schedulingItemId.set(null);
        this.coinPricingService.refreshAfterSpend();
        const brandProfileId = this.campaign()?.brandProfileId;
        if (brandProfileId) {
          this.contentItemService.refresh(brandProfileId, this.campaignId()).subscribe();
          this.scheduledPostService.refresh(brandProfileId, this.campaignId()).subscribe();
        }
      },
      error: err => {
        this.schedulingItemBusy.set(false);
        this.coinPricingService.refreshAfterSpend();
        this.showSpendError(err, 'تعذّر جدولة المنشور.');
      },
    });
  }

  protected openSelectedSchedulePanel(): void {
    if (this.selectedCount() === 0) return;
    const soon = new Date(Date.now() + 60 * 60 * 1000);
    this.scheduleDate.set(soon.toISOString().slice(0, 10));
    this.scheduleTime.set(soon.toISOString().slice(11, 16));
    this.selectedSchedulePanelOpen.set(true);
  }

  protected cancelSelectedSchedule(): void {
    this.selectedSchedulePanelOpen.set(false);
  }

  /** Schedules exactly the selected posts (as opposed to the single per-item panel, or the bulk
   *  button's "every eligible post") — each still routes to its own platform's connected account
   *  automatically, same as the bulk action, but only for the posts the user actually picked. Runs
   *  every request in parallel and reports a per-post outcome, reusing the same `scheduleResult`
   *  summary the bulk action renders so there's one consistent place to read the results. */
  protected submitSelectedSchedule(): void {
    if (!this.scheduleDate() || !this.scheduleTime() || this.scheduleTimeTooSoon()
      || this.bulkSelectedScheduling() || !this.perms.canEdit()) return;
    const items = this.items().filter(i => this.selectedItemIds().has(i.contentItemId));
    if (items.length === 0) return;

    this.bulkSelectedScheduling.set(true);
    const scheduledAt = `${this.scheduleDate()}T${this.scheduleTime()}:00`;

    interface Outcome {
      item: ContentItemSummary; succeeded: boolean; error?: string; scheduledPostId?: string; scheduledAtResult?: string;
    }

    const requests = items.map((item): import('rxjs').Observable<Outcome> => {
      const account = this.accountsForPlatform(item.platform)[0];
      if (!account) {
        return of<Outcome>({ item, succeeded: false, error: `لا يوجد حساب متصل لـ${this.platformLabels[item.platform]}` });
      }
      return this.scheduledPostService.schedule({
        contentItemId: item.contentItemId,
        visualAssetId: item.visualAssetId ?? undefined,
        socialAccountId: account.socialAccountId,
        scheduledAt,
      }).pipe(
        map((res): Outcome => ({
          item, succeeded: !!res.data, scheduledPostId: res.data?.scheduledPostId, scheduledAtResult: res.data?.scheduledAt,
        })),
        catchError((err: unknown) => of<Outcome>({ item, succeeded: false, error: extractApiErrorMessage(err, 'تعذّر جدولة المنشور.') })),
      );
    });

    forkJoin(requests).subscribe(outcomes => {
      this.bulkSelectedScheduling.set(false);
      this.coinPricingService.refreshAfterSpend();

      const succeeded = outcomes.filter(o => o.succeeded).length;
      this.scheduleResult.set({
        succeeded,
        failed: outcomes.length - succeeded,
        skipped: 0,
        results: outcomes.map(o => ({
          contentItemId: o.item.contentItemId,
          platform: o.item.platform,
          succeeded: o.succeeded,
          skipped: false,
          error: o.error ?? null,
          scheduledPostId: o.scheduledPostId ?? null,
          scheduledAt: o.scheduledAtResult ?? null,
        })),
      });

      this.clearSelection();
      const brandProfileId = this.campaign()?.brandProfileId;
      if (brandProfileId) {
        this.contentItemService.refresh(brandProfileId, this.campaignId()).subscribe();
        this.scheduledPostService.refresh(brandProfileId, this.campaignId()).subscribe();
      }
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
