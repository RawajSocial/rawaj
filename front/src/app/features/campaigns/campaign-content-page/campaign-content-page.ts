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
import { ConfirmDialogService } from '../../../services/confirm-dialog.service';
import { extractApiErrorMessage } from '../../../core/auth/api-error.util';
import { parseInsufficientCoins } from '../../../core/auth/coin-error.util';
import { CoinCostHint } from '../../../shared/components/coin-cost-hint/coin-cost-hint';
import { TooltipDirective } from '../../../shared/directives/tooltip.directive';
import { GetCampaignResponse, ScheduleCampaignPostResult } from '../../../model/campaign.model';
import { GetRunStatusResponse } from '../../../model/ai-pipeline.model';
import { ContentItemSummary } from '../../../model/content-item.model';
import { SocialAccountSummary } from '../../../model/social-account.model';
import { cairoLocalToUtcIso, formatCairoDate, formatCairoTime, utcIsoToCairoLocalParts } from '../../../shared/utils/cairo-time.util';
import { facebookPostUrl } from '../../../shared/utils/social-links.util';

const CONTENT_TYPE_LABELS: Record<ContentItemSummary['contentType'], string> = {
  Post: 'بوست', Story: 'قصة', ReelScript: 'ريل', AdCopy: 'إعلان', Blog: 'مقال', Caption: 'كابشن',
};

const PLATFORM_LABELS: Record<ContentItemSummary['platform'], string> = {
  Instagram: 'إنستغرام', Facebook: 'فيسبوك',
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
  private readonly confirmDialogService = inject(ConfirmDialogService);
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

  /** `run().progress.imagesTotal` at the moment the in-flight "توليد المحتوى" request was fired —
   *  `null` when no request is in flight. A campaign's pipeline run is reused across every "generate
   *  another batch" click (its `runId` never changes — see currentBatchItemIds' remarks), so a poll
   *  landing right after this click can be "fresh" (a real, post-click response) while still carrying
   *  the *previous* batch's totals: the backend only just flipped ContentPlan back to Pending, it
   *  hasn't actually re-run and fanned out this batch's own ContentImage stages yet. The total only
   *  becomes trustworthy once it's grown past what it was at click time. */
  private readonly imagesTotalAtClick = signal<number | null>(null);

  /** `run()`'s exact object reference at click time — `AiPipelineService.applyRunUpdate` always
   *  assigns a fresh object on every accepted update, so `run() !== runRefAtClick` is a reliable
   *  "has anything at all arrived since I clicked" check. This is what `imagesTotalAtClick` alone
   *  can't tell: the run held right at click time is virtually always sitting at a *terminal* status
   *  already (the previous batch's own finished state — this run is reused, never recreated), so a
   *  naive "bail out once isTerminal()" check would fire immediately on that same stale object,
   *  before any request had even reached the server. Checking this first is what lets isTerminal()
   *  below be trusted only once it's actually describing something new. Plain field, not a signal:
   *  it only needs to be read inside awaitingFreshBatchStatus, never to trigger it on its own. */
  private runRefAtClick: GetRunStatusResponse | null = null;

  /** True from the moment "توليد المحتوى" is clicked until a genuinely new run update has arrived
   *  (see runRefAtClick) AND that update shows either `imagesTotal` grown past imagesTotalAtClick (the
   *  new batch has actually been planned) or a terminal status (nothing more is coming — a failure
   *  shouldn't leave this stuck forever). The banner stays up throughout regardless (see
   *  `generating`/`requestInFlight`), but anything that displays a specific number from `run()` should
   *  check this first rather than show a total that's about to change. */
  protected readonly awaitingFreshBatchStatus = computed(() => {
    const captured = this.imagesTotalAtClick();
    if (captured === null) return false;

    const run = this.aiPipelineService.run();
    if (run === this.runRefAtClick) return true; // nothing has arrived yet — definitely still stale

    const total = run?.progress?.imagesTotal ?? 0;
    return total <= captured && !this.aiPipelineService.isTerminal();
  });

  /** True while the campaign's pipeline run hasn't reached a terminal status — real, server-derived
   *  state, not a local guess. This is what closes the reload double-spend gap: a reload picks this up
   *  from the very first poll tick (started in `load()` whenever the campaign has a
   *  `currentPipelineRunId`), so a batch already running from a previous tab/session disables the
   *  button here too, not just in the tab that started it.
   *
   *  Deliberately reads `run.status` rather than scanning `run.stages` for a Pending/Running
   *  ContentPlan/ContentImage entry (an earlier version of this did exactly that): each of a batch's
   *  ContentImage stages settles in its own isolated backend DB scope and independently re-queries
   *  every sibling before pushing its own snapshot (see PipelineOrchestrator's own remarks on this),
   *  so with N images generating concurrently, N of these racy per-stage snapshots land in quick
   *  succession and can — in an order that doesn't match which one is actually more complete —
   *  transiently look like nothing is left pending, flickering this false and back on every single
   *  image. `run.status` itself is untouched by that per-stage race: it's only ever recomputed once,
   *  authoritatively, after a full dispatch wave finishes (PipelineOrchestrator.AdvanceAsync, after its
   *  `Task.WhenAll`), so it stays `Running` continuously for the run's entire duration and only
   *  changes once, for real, at true completion. By the time this page is usable the run has already
   *  passed its strategy phase (approved and settled), so "not terminal" here always specifically means
   *  "a content batch is what's in flight". */
  protected readonly contentBatchInFlight = computed(() => {
    const run = this.aiPipelineService.run();
    if (!run) return false;
    return !this.aiPipelineService.isTerminal();
  });

  protected readonly generating = computed(() => this.requestInFlight() || this.contentBatchInFlight());

  /** Informational only — the backend worker retries a coins-blocked stage on its own poll cycle
   *  once a top-up lands, so there is nothing to click here (same as the strategy page's note). */
  protected readonly awaitingCoins = computed(() => this.aiPipelineService.run()?.status === 'AwaitingCoins');

  /** ContentItem ids that belong to the most recently generated batch — every post in one batch is
   *  inserted in the same instant (see `batchItems` below), so the batch a post belongs to is exactly
   *  "shares the newest `createdAt` seen for this campaign." Derived straight off `items()` rather than
   *  off the pipeline run's `ContentImage` stages: a campaign's `AiPipelineRun` is reused across every
   *  "generate another batch" click (see `GenerateCampaignContentCommandHandler` — it resets the one
   *  `ContentPlan` row rather than creating a second run), so `run.stages` keeps every earlier batch's
   *  `ContentImage` stages forever with nothing marking which ones are from the latest click. Grouping
   *  by `createdAt` instead sidesteps that entirely — it doesn't matter how many batches share the run,
   *  only the newest cluster of items is ever "current". */
  private readonly currentBatchItemIds = computed(() => {
    const all = this.items();
    if (all.length === 0) return new Set<string>();

    let latestCreatedAt = all[0].createdAt;
    for (const item of all) {
      if (item.createdAt > latestCreatedAt) latestCreatedAt = item.createdAt;
    }

    return new Set(all.filter(i => i.createdAt === latestCreatedAt).map(i => i.contentItemId));
  });

  /** This run's own posts, in their planned running order (`suggestedPostAt` — the day/hour the
   *  content plan gave each one — not creation time: the whole batch is inserted in one instant, so
   *  every post in it shares the same `createdAt` and can't be ordered by that). This is what lets a
   *  specific post replace a specific skeleton slot instead of skeletons just shrinking from one
   *  end: slot N is always "the Nth post in this run's plan", whether it's still a skeleton or
   *  already has its image. Ties (same suggestedPostAt, or neither set) fall back to id order only
   *  to keep the sort stable across recomputes — not a claim that this order is meaningful. */
  protected readonly batchItems = computed(() => {
    const ids = this.currentBatchItemIds();
    return this.items()
      .filter(i => ids.has(i.contentItemId))
      .sort((a, b) => {
        const at = a.suggestedPostAt ? new Date(a.suggestedPostAt).getTime() : Number.POSITIVE_INFINITY;
        const bt = b.suggestedPostAt ? new Date(b.suggestedPostAt).getTime() : Number.POSITIVE_INFINITY;
        return at !== bt ? at - bt : a.contentItemId.localeCompare(b.contentItemId);
      });
  });

  /** Everything from an earlier batch — rendered as plain ready cards, unaffected by whatever the
   *  current run is doing (never shown as a skeleton, even while generating() is true). */
  protected readonly historyItems = computed(() => {
    const ids = this.currentBatchItemIds();
    return this.items().filter(i => !ids.has(i.contentItemId));
  });

  /** One list to render: this run's own posts (in their planned slot order) first, then everything
   *  from earlier batches after — "skeletons on top" of already-generated history, not mixed in. */
  protected readonly displayItems = computed(() => [...this.batchItems(), ...this.historyItems()]);

  /** True only for a post that's both part of the run currently tracked here AND still missing its
   *  image — the one case that renders as a skeleton instead of the real card. A history item
   *  without an image (the standalone "generated with no image" case, unrelated to any run) always
   *  renders as the real card, which already has its own "missing image, retry" state built in. */
  protected isBatchPending(item: ContentItemSummary): boolean {
    return !item.imageUrl && this.currentBatchItemIds().has(item.contentItemId);
  }

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

  /** Reschedule panel for an already-scheduled post — a separate mode from the initial-schedule
   *  panel above (no account picker; it's changing the time on an existing ScheduledPost, not
   *  creating a new one), reusing the same scheduleDate/scheduleTime fields since only one of the
   *  two panels is ever open for a given card at a time. */
  protected readonly reschedulingItemId = signal<string | null>(null);

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
    const picked = new Date(cairoLocalToUtcIso(date, time));
    if (isNaN(picked.getTime())) return false;
    return picked.getTime() < Date.now() + 10 * 60 * 1000;
  });

  /** Content items with an active (Pending or Published) scheduled post, keyed to the post itself
   *  (not just a Set of ids) — the backend deliberately leaves ContentItem.Status as Approved after
   *  scheduling (dedup happens server-side against ScheduledPosts, not content status), so without
   *  this "approved" would keep including posts that are already scheduled, overstating the
   *  coin-cost hint and, once nothing new is left to schedule, surfacing a generic-looking error for
   *  what's actually a no-op. Failed/cancelled posts are NOT active, since the backend allows
   *  re-scheduling those. Keeping the actual post (not just the id) lets the card show its real
   *  scheduled time instead of just hiding the AI's suggestion once one exists. */
  private readonly activeScheduledPostByContentItemId = computed(() => new Map(
    this.scheduledPostService.byCampaign(this.campaignId())()
      .filter(p => p.status === 'scheduled' || p.status === 'published')
      .map(p => [p.contentItemId, p] as const),
  ));

  private readonly activelyScheduledContentItemIds = computed(() =>
    new Set(this.activeScheduledPostByContentItemId().keys()),
  );

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

  /** The AI's suggested time is only meaningful before the post has an actual scheduled time of its
   *  own — once it's genuinely scheduled, the real ScheduledPost.scheduledAt (below) is what will
   *  actually happen, not this suggestion. */
  protected showsSuggestedTime(item: ContentItemSummary): boolean {
    return !!item.suggestedPostAt && !this.activelyScheduledContentItemIds().has(item.contentItemId);
  }

  protected activeScheduledPost(item: ContentItemSummary) {
    return this.activeScheduledPostByContentItemId().get(item.contentItemId) ?? null;
  }

  /** Scheduled but not yet actually sent/fired — still cancellable/reschedulable. */
  protected isPendingSchedule(item: ContentItemSummary): boolean {
    return this.activeScheduledPost(item)?.status === 'scheduled';
  }

  /** Genuinely already live on the platform — nothing left to approve, decline, or reschedule from
   *  here; that would just be pretending to undo something that already happened. */
  protected isLive(item: ContentItemSummary): boolean {
    return this.activeScheduledPost(item)?.status === 'published';
  }

  /** Re-approving an already-approved item (scheduled or not) is a no-op that only invites
   *  confusion — hide the button once there's nothing left for it to do. */
  protected showApprove(item: ContentItemSummary): boolean {
    return item.status !== 'Approved' && item.status !== 'Published';
  }

  /** Hidden only once the post has actually gone out — there's nothing to decline/cancel by then.
   *  While still schedulable-but-pending, the SAME button cancels the schedule instead of rejecting
   *  the content (see declineLabel/onDecline). */
  protected showDecline(item: ContentItemSummary): boolean {
    return !this.isLive(item);
  }

  /** Only Facebook post ids are usable directly as a permalink (facebook.com/{id} redirects
   *  correctly) — Instagram's publish API returns a numeric media id, not the shortcode a real
   *  instagram.com/p/ link needs, so there's no link offered there. */
  protected liveFacebookUrl(item: ContentItemSummary): string | null {
    const scheduledPost = this.activeScheduledPost(item);
    if (!scheduledPost || !this.isLive(item) || item.platform !== 'Facebook' || !scheduledPost.postId) return null;
    return facebookPostUrl(scheduledPost.postId);
  }

  protected declineLabel(item: ContentItemSummary): string {
    return this.isPendingSchedule(item) ? 'إلغاء الجدولة' : 'رفض';
  }

  protected formatSuggestedTime(iso: string): string {
    return `${formatCairoDate(iso, { day: 'numeric', month: 'short' })} · ${formatCairoTime(iso)}`;
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

    // Closes the gap described in generateContent(): once imagesTotal has genuinely grown past what
    // it was at click time (or the run's hit a terminal status) — not just any update landing, which
    // can still carry the previous batch's stale total for a moment — requestInFlight can safely drop
    // and contentBatchInFlight() takes over generating() from here.
    effect(() => {
      if (this.requestInFlight() && !this.awaitingFreshBatchStatus()) {
        this.requestInFlight.set(false);
        this.imagesTotalAtClick.set(null);
      }
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
    this.requestInFlight.set(false);
    this.imagesTotalAtClick.set(null);
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

    // Captured now, before anything async — see imagesTotalAtClick/runRefAtClick's remarks for why
    // this has to happen at the same instant as requestInFlight rather than once the POST resolves:
    // whatever the service already holds as of right now is necessarily the *previous* batch's (this
    // campaign's run gets reused, not recreated, on a second-or-later "generate" click).
    const runAtClick = this.aiPipelineService.run();
    this.requestInFlight.set(true);
    this.imagesTotalAtClick.set(runAtClick?.progress?.imagesTotal ?? 0);
    this.runRefAtClick = runAtClick;

    this.campaignService.generateContent(this.campaignId(), {
      postCount: this.postCount(),
      language: 'Ar',
      includeImages: true,
    }).subscribe({
      // Fire-and-track: the request resolves as soon as the batch is *runnable*, not once it's
      // done — nothing to refresh yet. Watching the run (poll + SignalR push) is what surfaces
      // progress and, via the imagesCompleted effect above, each post's card as its image lands.
      //
      // requestInFlight deliberately stays true here rather than being cleared immediately: startPolling's
      // first fetch is itself async, and clearing it now would leave a real gap — generating() would
      // read false and items() would still be empty, hitting the "no posts yet" empty state for a
      // moment before the first status arrives. The effect below clears it once imagesTotal actually
      // grows past imagesTotalAtClick.
      next: res => {
        this.coinPricingService.refreshAfterSpend();
        if (res.data) {
          this.aiPipelineService.startPolling(res.data.runId);
        } else {
          this.requestInFlight.set(false);
          this.imagesTotalAtClick.set(null);
        }
      },
      error: err => {
        this.requestInFlight.set(false);
        this.imagesTotalAtClick.set(null);
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

  /** The decline button is a single control whose meaning changes with state: while the post is
   *  still pending (not yet fired), it only cancels the schedule — the content stays Approved and
   *  the button then reverts to plain "رفض" for a second, separate click to actually reject it.
   *  Keeping these as two distinct steps (rather than one bundled action) means declining never
   *  silently does two different things depending on what state it happened to catch the post in. */
  protected onDeclineClick(item: ContentItemSummary): void {
    if (this.isPendingSchedule(item)) {
      void this.cancelSchedule(item);
    } else {
      this.review(item, false);
    }
  }

  protected async cancelSchedule(item: ContentItemSummary): Promise<void> {
    const scheduledPost = this.activeScheduledPost(item);
    if (!scheduledPost || this.busyItemId() || !this.perms.canEdit()) return;

    const confirmed = await this.confirmDialogService.confirm(
      'سيتم إلغاء جدولة هذا المنشور (وعلى المنصة نفسها إن كان قد أُرسل إليها بالفعل)، ولن تُسترد الكوينات المستخدمة في الجدولة. هل تريد المتابعة؟',
      { title: 'إلغاء الجدولة', confirmLabel: 'إلغاء الجدولة', cancelLabel: 'تراجع', variant: 'danger' },
    );
    if (!confirmed) return;

    this.busyItemId.set(item.contentItemId);
    this.scheduledPostService.cancel(scheduledPost.id).subscribe({
      next: () => {
        this.scheduledPostService.remove(scheduledPost.id);
        this.busyItemId.set(null);
      },
      error: err => {
        this.busyItemId.set(null);
        this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر إلغاء جدولة المنشور.'), { variant: 'error' });
      },
    });
  }

  /** Deletes an already-live post from the platform itself — irreversible, unlike cancelSchedule
   *  which only revokes a not-yet-fired schedule. Resets the content item back to Draft server-side
   *  (see TakeDownScheduledPostCommandHandler) so it re-refreshes here as an editable draft. */
  protected async takeDownPost(item: ContentItemSummary): Promise<void> {
    const scheduledPost = this.activeScheduledPost(item);
    if (!scheduledPost || this.busyItemId() || !this.perms.canEdit()) return;

    const confirmed = await this.confirmDialogService.confirm(
      'سيتم حذف هذا المنشور نهائيًا من المنصة، ولا يمكن التراجع عن هذا الإجراء. سيعود المحتوى مسودة يمكنك تعديلها وجدولتها من جديد.',
      { title: 'سحب المنشور', confirmLabel: 'سحب المنشور', cancelLabel: 'تراجع', variant: 'danger' },
    );
    if (!confirmed) return;

    this.busyItemId.set(item.contentItemId);
    this.scheduledPostService.takeDown(scheduledPost.id).subscribe({
      next: () => {
        this.scheduledPostService.remove(scheduledPost.id);
        this.busyItemId.set(null);
        const brandProfileId = this.campaign()?.brandProfileId;
        if (brandProfileId) this.contentItemService.refresh(brandProfileId, this.campaignId()).subscribe();
      },
      error: err => {
        this.busyItemId.set(null);
        this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر سحب المنشور.'), { variant: 'error' });
      },
    });
  }

  protected startReschedule(item: ContentItemSummary): void {
    if (!this.perms.canEdit()) return;
    const scheduledPost = this.activeScheduledPost(item);
    if (!scheduledPost) return;
    const { date, time } = utcIsoToCairoLocalParts(scheduledPost.scheduledAt);
    this.reschedulingItemId.set(item.contentItemId);
    this.scheduleDate.set(date);
    this.scheduleTime.set(time);
  }

  protected cancelReschedule(): void {
    this.reschedulingItemId.set(null);
  }

  protected submitReschedule(item: ContentItemSummary): void {
    const scheduledPost = this.activeScheduledPost(item);
    if (!scheduledPost || !this.scheduleDate() || !this.scheduleTime() || this.scheduleTimeTooSoon()
      || this.schedulingItemBusy() || !this.perms.canEdit()) return;

    this.schedulingItemBusy.set(true);
    const scheduledAt = cairoLocalToUtcIso(this.scheduleDate(), this.scheduleTime());
    this.scheduledPostService.reschedule(scheduledPost.id, scheduledAt).subscribe({
      next: () => {
        this.schedulingItemBusy.set(false);
        this.reschedulingItemId.set(null);
      },
      error: err => {
        this.schedulingItemBusy.set(false);
        this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر إعادة جدولة المنشور.'), { variant: 'error' });
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
        // The text half can succeed and be saved server-side even when the image half then fails
        // (see RegenerateContentItemCommandHandler) — refresh so the card doesn't keep showing the
        // stale pre-remake caption after an error that was really only about the photo.
        const brandProfileId = this.campaign()?.brandProfileId;
        if (brandProfileId) this.contentItemService.refresh(brandProfileId, this.campaignId()).subscribe();
      },
    });
  }

  /** How many results in a bulk-schedule response were published immediately (past-due AI time,
   *  "publish now" chosen) rather than handed off to a platform's native scheduler. */
  protected publishedNowCountIn(results: ScheduleCampaignPostResult[]): number {
    return results.filter(r => r.succeeded && r.status === 'Published').length;
  }

  /** Counts posts among `approvedItems()` whose AI-suggested time has already passed (or never
   *  had one) — the exact set the backend would otherwise silently push forward to "now + 20 min,
   *  staggered" without telling anyone (see SchedulingWindow.ClampForward). */
  private pastDueCount(): number {
    const cutoff = Date.now() + 10 * 60 * 1000;
    return this.approvedItems().filter(i => !i.suggestedPostAt || new Date(i.suggestedPostAt).getTime() < cutoff).length;
  }

  protected async schedulePosts(): Promise<void> {
    if (this.scheduling() || this.approvedItems().length === 0 || !this.perms.canEdit()) return;

    const staleCount = this.pastDueCount();
    let publishPastDueNow = false;
    if (staleCount > 0) {
      publishPastDueNow = await this.confirmDialogService.confirm(
        `الوقت المقترح لـ${staleCount} من المنشورات قد مضى بالفعل. هل تريد نشرها الآن، أم جدولتها تلقائيًا لأقرب وقت متاح؟`,
        { title: 'وقت منشورات قد مضى', confirmLabel: 'انشرها الآن', cancelLabel: 'جدولة تلقائية' },
      );
    }

    this.scheduling.set(true);
    this.scheduleResult.set(null);
    this.campaignService.schedulePosts(this.campaignId(), publishPastDueNow).subscribe({
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
    const { date, time } = utcIsoToCairoLocalParts(soon.toISOString());
    this.scheduleDate.set(date);
    this.scheduleTime.set(time);
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
    const scheduledAt = cairoLocalToUtcIso(this.scheduleDate(), this.scheduleTime());
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

  /** Publishes this one post immediately, bypassing the date/time pickers entirely (and the
   *  "10 minutes out" scheduling window they're subject to) — same account requirement as
   *  `submitSchedulePost`, but skips straight to a real publish instead of a native-scheduler
   *  handoff. */
  protected submitSchedulePostNow(item: ContentItemSummary): void {
    const accountId = this.scheduleAccountId();
    if (!accountId || this.schedulingItemBusy() || !this.perms.canEdit()) return;

    this.schedulingItemBusy.set(true);
    this.scheduledPostService.schedule({
      contentItemId: item.contentItemId,
      visualAssetId: item.visualAssetId ?? undefined,
      socialAccountId: accountId,
      scheduledAt: new Date().toISOString(),
      publishNow: true,
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
        this.showSpendError(err, 'تعذّر نشر المنشور.');
      },
    });
  }

  protected openSelectedSchedulePanel(): void {
    if (this.selectedCount() === 0) return;
    const soon = new Date(Date.now() + 60 * 60 * 1000);
    const { date, time } = utcIsoToCairoLocalParts(soon.toISOString());
    this.scheduleDate.set(date);
    this.scheduleTime.set(time);
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
    const scheduledAt = cairoLocalToUtcIso(this.scheduleDate(), this.scheduleTime());

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
        `تحتاج ${shortfall.required.toLocaleString('ar-EG')} كوين لإتمام هذا الإجراء، ورصيدك الحالي ${shortfall.balance.toLocaleString('ar-EG')} كوين.`,
        { variant: 'warning', actionLabel: 'شحن الرصيد', actionLink: ['/dashboard/billing'] },
      );
      return;
    }
    this.errorModalService.show(extractApiErrorMessage(err, fallback), { variant: 'error' });
  }
}
