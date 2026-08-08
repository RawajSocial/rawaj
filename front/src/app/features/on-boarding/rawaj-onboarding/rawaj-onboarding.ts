import { Component, DestroyRef, computed, effect, inject, signal, WritableSignal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { OnboardingSidebar } from '../onboarding-sidebar/onboarding-sidebar';
import { OnboardingStepOne } from '../onboarding-step-one/onboarding-step-one';
import { OnboardingStepCampaignBrief } from '../onboarding-step-campaign-brief/onboarding-step-campaign-brief';
import { OnboardingStepThree } from '../onboarding-step-three/onboarding-step-three';
import { SeoService } from '../../../services/seo.service';
import { OnboardingStepFour } from '../onboarding-step-four/onboarding-step-four';
import { OnboardingStepFive } from '../onboarding-step-five/onboarding-step-five';
import { OnboardingStepSix } from '../onboarding-step-six/onboarding-step-six';
import { OnboardingStepSeven } from '../onboarding-step-seven/onboarding-step-seven';
import { OnboardingSuccess } from '../onboarding-success/onboarding-success';
import { OnboardingPlanApproval } from '../onboarding-plan-approval/onboarding-plan-approval';
import { BrandProfileService } from '../../../services/brand-profile.service';
import { BrandContextService } from '../../../services/brand-context.service';
import { CampaignService } from '../../../services/campaign.service';
import { ErrorModalService } from '../../../services/error-modal.service';
import { ConfirmDialogService } from '../../../services/confirm-dialog.service';
import { extractApiErrorMessage } from '../../../core/auth/api-error.util';
import { CreateCampaignInput, UpdateCampaignInput } from '../../../model/campaign.model';

/** Frontend platform slugs the backend's SocialPlatform enum actually accepts (case-insensitively).
 *  Anything else the wizard collects (e.g. snapchat, whatsapp) has no campaign-level equivalent yet
 *  and is dropped rather than sent, since CreateCampaignCommandHandler throws on an unknown name. */
const BACKEND_PLATFORMS = new Set(['instagram', 'facebook', 'tiktok', 'twitter', 'youtube', 'linkedin']);

/** How long to wait after the last keystroke/change before autosaving the draft to the database. */
const AUTOSAVE_DEBOUNCE_MS = 1000;

@Component({
  selector: 'app-rawaj-onboarding',
  imports: [
    OnboardingSidebar,
    OnboardingStepOne,
    OnboardingStepCampaignBrief,
    OnboardingStepThree,
    OnboardingStepFour,
    OnboardingStepFive,
    OnboardingStepSix,
    OnboardingStepSeven,
    OnboardingSuccess,
    OnboardingPlanApproval,
  ],
  templateUrl: './rawaj-onboarding.html',
  styleUrl: './rawaj-onboarding.css',
})
export class RawajOnboarding {
  protected readonly totalSteps = 7;
  protected readonly currentStep: WritableSignal<number>;
  protected readonly onboardingData = signal<OnboardingData>({});
  /** Pure UI position — not business data, safe to keep client-side. */
  private readonly stepStorageKey = 'rawaj.onboarding.step';
  /** Points at the in-progress draft campaign so a page refresh mid-wizard can resume it. The
   *  actual answers live server-side (Campaign.BriefJson) — this is only an id, never the data
   *  itself, so it can't go stale the way the old localStorage-mirrored blob did. */
  private readonly draftIdKey = 'rawaj.onboarding.draftId';
  private readonly seo = inject(SeoService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly brandProfileService = inject(BrandProfileService);
  private readonly brandContextService = inject(BrandContextService);
  private readonly campaignService = inject(CampaignService);
  private readonly errorModalService = inject(ErrorModalService);
  private readonly confirmDialogService = inject(ConfirmDialogService);

  /** The brand profile this campaign will be assigned to — defaults to whichever the header's
   *  global brand switcher already has selected, but the sidebar lets the user pick a different
   *  one of their brands when they have more than one. */
  protected readonly brandProfiles = this.brandProfileService.profiles;
  protected readonly selectedBrandProfileId = signal<string | null>(null);
  protected readonly creatingCampaign = signal(false);
  protected readonly campaignId = signal<string | null>(null);
  /** Set when `createDraft()` fails so `finishOnboarding()` can report the real reason instead of
   *  always blaming a missing brand profile. */
  private readonly draftCreationError = signal<string | null>(null);

  protected readonly selectedBrandProfile = computed(() =>
    this.brandProfiles().find(p => p.id === this.selectedBrandProfileId()),
  );

  /** Guards the autosave effect until the draft has actually been created/resumed — otherwise it
   *  would fire on the initial empty `onboardingData()` and race the resume fetch. Also blocks the
   *  wizard's steps from rendering at all until then (see rawaj-onboarding.html), closing the
   *  window where a user could edit a field while the resume GET is still in flight and have that
   *  edit silently overwritten once the fetched brief lands. */
  protected readonly draftReady = signal(false);
  private autosaveTimer: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    this.currentStep = signal(1);
    this.seo.setPageSeo({
      title: 'إعداد الحساب | رواج',
      description: 'ابدأ إعداد حسابك في رواج عبر خطوات مخصصة لنوع نشاطك التجاري.',
      keywords: 'رواج, إعداد الحساب, نوع الحساب, التسويق الذكي',
      path: '/on-boarding',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });

    // `resume`/`fresh` are one-shot entry signals, not a persistent mode — read once here and
    // stripped from the URL the moment a draft is established (see `stripEntryQueryParams`). Before
    // that stripping existed, `fresh=1` stayed in the address bar forever, so a plain refresh
    // mid-wizard re-wiped and recreated the draft on every reload, even on a brand-new attempt.
    const isFresh = this.route.snapshot.queryParamMap.get('fresh') === '1';
    const resumeId = this.route.snapshot.queryParamMap.get('resume');
    if (isFresh) {
      localStorage.removeItem(this.draftIdKey);
      localStorage.removeItem(this.stepStorageKey);
    }

    if (resumeId) {
      // Resuming a specific campaign by id (from a dashboard card, not a same-tab refresh) — the
      // brand it belongs to comes from the campaign itself, not from whatever the global brand
      // switcher happens to have selected right now.
      this.resumeSpecificDraft(resumeId);
    } else {
      const defaultBrandId = this.brandContextService.selectedBrandProfileId() ?? this.brandProfiles()[0]?.id ?? null;
      this.selectedBrandProfileId.set(defaultBrandId);
      if (defaultBrandId) {
        this.initializeDraft(defaultBrandId, isFresh);
      } else if (this.brandProfiles().length === 0) {
        this.brandProfileService.refresh().subscribe(res => {
          const first = res.data?.[0]?.brandProfileId;
          if (first) {
            this.selectedBrandProfileId.set(first);
            this.initializeDraft(first, isFresh);
          } else {
            this.redirectToCreateBrandProfile();
          }
        });
      }
    }

    effect(() => {
      const step = this.currentStep();
      // Guarded the same way the autosave effect below it is guarded: this fires on its first tick
      // with the signal's initial value (1), before an async resume (localStorage or ?resume=<id>)
      // has had a chance to load the real step — without this guard, that first tick stomped the
      // saved step to '1' before `loadSavedStep()` ever got to read it back.
      if (!this.draftReady()) return;
      if (step >= 1 && step <= this.totalSteps) {
        localStorage.setItem(this.stepStorageKey, String(step));
      }
    });

    effect(() => {
      const data = this.onboardingData();
      if (!this.draftReady()) return;
      const id = this.campaignId();
      if (!id) return;

      if (this.autosaveTimer) clearTimeout(this.autosaveTimer);
      this.autosaveTimer = setTimeout(() => this.flushAutosave(id, data), AUTOSAVE_DEBOUNCE_MS);
    });

    this.destroyRef.onDestroy(() => {
      if (this.autosaveTimer) clearTimeout(this.autosaveTimer);
    });
  }

  /** Either resumes the draft campaign pointed at by `draftIdKey` (a same-tab refresh mid-wizard),
   *  or creates a brand-new Draft campaign row immediately so every step's answers are autosaved to
   *  the database (Campaign.BriefJson) from the start, instead of only living in localStorage until
   *  a single "create" call at the very end. */
  private initializeDraft(brandProfileId: string, isFresh: boolean): void {
    const existingDraftId = isFresh ? null : localStorage.getItem(this.draftIdKey);
    if (existingDraftId) {
      this.campaignService.getCampaign(existingDraftId).subscribe({
        next: res => {
          const campaign = res.data;
          if (campaign && campaign.status === 'Draft' && !campaign.planApprovedAt) {
            this.campaignId.set(existingDraftId);
            this.onboardingData.set(this.parseBriefJson(campaign.briefJson));
            this.currentStep.set(this.loadSavedStep());
            this.draftReady.set(true);
            this.stripEntryQueryParams();
          } else {
            localStorage.removeItem(this.draftIdKey);
            this.createDraft(brandProfileId);
          }
        },
        error: () => {
          localStorage.removeItem(this.draftIdKey);
          this.createDraft(brandProfileId);
        },
      });
    } else {
      this.createDraft(brandProfileId);
    }
  }

  private createDraft(brandProfileId: string): void {
    const input = this.buildCreateCampaignInput(brandProfileId);
    this.campaignService.create(input).subscribe({
      next: res => {
        if (res.data) {
          localStorage.setItem(this.draftIdKey, res.data.campaignId);
          this.campaignId.set(res.data.campaignId);
          this.draftCreationError.set(null);
        }
        this.draftReady.set(true);
        this.stripEntryQueryParams();
      },
      error: err => {
        // The wizard still works locally even if the initial draft row couldn't be created (e.g.
        // the tenant already hit its plan's monthly campaign cap, or isn't activated yet) —
        // recorded here so finishOnboarding() can surface the REAL reason instead of always
        // assuming "no brand profile", which used to be the only message it ever showed.
        this.draftCreationError.set(extractApiErrorMessage(err, 'تعذّر إنشاء الحملة. حاول مرة أخرى.'));
        this.draftReady.set(true);
        this.stripEntryQueryParams();
      },
    });
  }

  /** Resumes a specific campaign by id — the entry point a Draft campaign's dashboard card links
   *  to, as opposed to `initializeDraft`'s same-tab-refresh path which only ever had a localStorage
   *  pointer to go on. Only a Draft, not-yet-approved campaign is resumable this way; anything else
   *  (deleted, already approved, belongs to another tenant) bounces back to the dashboard rather
   *  than silently falling through into creating an unrelated new draft. */
  private resumeSpecificDraft(campaignId: string): void {
    this.campaignService.getCampaign(campaignId).subscribe({
      next: res => {
        const campaign = res.data;
        if (!campaign || campaign.status !== 'Draft' || campaign.planApprovedAt) {
          this.errorModalService.show(
            'لم يعد من الممكن متابعة هذه الحملة.', { variant: 'warning' });
          void this.router.navigate(['/dashboard/campaigns']);
          return;
        }

        this.selectedBrandProfileId.set(campaign.brandProfileId);
        this.campaignId.set(campaignId);
        localStorage.setItem(this.draftIdKey, campaignId);
        this.onboardingData.set(this.parseBriefJson(campaign.briefJson));
        this.currentStep.set(campaign.onboardingCompletedAt ? this.totalSteps + 1 : this.loadSavedStep());
        this.draftReady.set(true);
        this.stripEntryQueryParams();
      },
      error: err => {
        this.errorModalService.show(
          extractApiErrorMessage(err, 'تعذّر تحميل الحملة.'), { variant: 'error' });
        void this.router.navigate(['/dashboard/campaigns']);
      },
    });
  }

  /** `resume`/`fresh` are one-shot entry signals — once the draft they pointed at is established,
   *  they're removed from the URL so a later refresh falls through to the (working) localStorage
   *  resume path instead of re-reading a stale `fresh=1` and wiping progress all over again. */
  private stripEntryQueryParams(): void {
    if (this.route.snapshot.queryParamMap.keys.length === 0) return;
    void this.router.navigate([], { relativeTo: this.route, queryParams: {}, replaceUrl: true });
  }

  private parseBriefJson(raw: string | null | undefined): OnboardingData {
    if (!raw) return {};
    try {
      return JSON.parse(raw) as OnboardingData;
    } catch {
      return {};
    }
  }

  private flushAutosave(campaignId: string, data: OnboardingData): void {
    this.campaignService.update(campaignId, this.buildUpdateCampaignInput(data)).subscribe({
      error: () => {
        // Best-effort autosave — a transient network failure here shouldn't interrupt the wizard;
        // the next change will simply retry, and finishOnboarding() does a final flush regardless.
      },
    });
  }

  /** The draft campaign's BrandProfileId isn't movable (it's not a patchable field, and brand-scoped
   *  resources hang off it), so switching brands mid-wizard can't just update the existing draft —
   *  it abandons it (archived, not left as an orphaned Draft row) and creates a fresh one under the
   *  new brand, carrying over whatever the user has already typed. */
  protected selectBrandProfile(id: string): void {
    if (id === this.selectedBrandProfileId()) return;

    const previousDraftId = this.campaignId();
    this.selectedBrandProfileId.set(id);
    this.draftReady.set(false);
    this.campaignId.set(null);
    localStorage.removeItem(this.draftIdKey);

    if (previousDraftId) this.campaignService.archive(previousDraftId).subscribe();
    this.createDraft(id);
  }

  private redirectToCreateBrandProfile(): void {
    this.errorModalService.show(
      'يجب إنشاء ملف علامة تجارية أولاً لاستخدام هذه الميزة.',
      { variant: 'warning', title: 'يلزم إنشاء ملف علامة تجارية' },
    );
    void this.router.navigate(['/dashboard/brand-profiles/new']);
  }

  /** The wizard's only way out before finishing all steps — until this existed, leaving meant
   *  finishing every step or hand-editing the URL. A pause, not an abandonment: progress is already
   *  autosaved (Campaign.BriefJson), so nothing is archived or discarded here — the draft is exactly
   *  where the user left it next time they open it from the campaigns list. */
  protected async confirmExit(): Promise<void> {
    const confirmed = await this.confirmDialogService.confirm(
      'سيتم حفظ تقدمك تلقائيًا، ويمكنك المتابعة لاحقًا من قائمة حملاتك.',
      { title: 'الخروج من إعداد الحملة', confirmLabel: 'الخروج', cancelLabel: 'متابعة الإعداد' },
    );
    if (confirmed) {
      // Autosave is debounced (AUTOSAVE_DEBOUNCE_MS) — without this flush, an edit made less than a
      // second before exiting never reaches the server: destroyRef's onDestroy below cancels the
      // pending timer as the component tears down for the route change, so the last change would
      // otherwise be silently lost despite the message above promising it's saved.
      this.flushPendingAutosave();
      void this.router.navigate(['/dashboard/campaigns']);
    }
  }

  private flushPendingAutosave(): void {
    if (this.autosaveTimer === null) return;
    clearTimeout(this.autosaveTimer);
    this.autosaveTimer = null;
    const id = this.campaignId();
    if (id) this.flushAutosave(id, this.onboardingData());
  }

  protected goToNextStep(): void {
    this.currentStep.update((step) => Math.min(this.totalSteps + 2, step + 1));
    window.scrollTo({ top: 0 });
  }

  protected goToPreviousStep(): void {
    this.currentStep.update((step) => Math.max(1, step - 1));
    window.scrollTo({ top: 0 });
  }

  /** Fires once the step-7 AI chat finishes. The draft campaign already exists (created when the
   *  wizard mounted) and has been autosaved along the way — this just does one final, immediate
   *  flush (skipping the debounce) so the last answers are guaranteed to be there before moving
   *  into the plan-approval step, which runs its research/diagnosis/strategy pipeline against it. */
  protected finishOnboarding(): void {
    const id = this.campaignId();
    if (!id) {
      // A brand-new tenant with zero brand profiles is redirected before the wizard ever mounts
      // its steps (see the constructor) — reaching here with no id almost always means the draft
      // campaign itself failed to create (plan limit reached, tenant not activated, network
      // error), so report THAT, not the "no brand profile" message that used to fire regardless.
      if (this.brandProfiles().length === 0) {
        this.redirectToCreateBrandProfile();
      } else {
        this.errorModalService.show(
          this.draftCreationError() ?? 'تعذّر إنشاء الحملة. حاول مرة أخرى أو أعد تحميل الصفحة.',
          { variant: 'error', title: 'تعذّر إنشاء الحملة' },
        );
      }
      return;
    }

    if (this.autosaveTimer) clearTimeout(this.autosaveTimer);
    this.creatingCampaign.set(true);
    // Sent once, here — not on every autosave tick — so a Draft campaign's
    // `onboardingCompletedAt` distinguishes "abandoned mid-wizard" from "reached strategy review",
    // which decides whether its dashboard card routes back into the wizard or into that review page.
    const input: UpdateCampaignInput = { ...this.buildUpdateCampaignInput(this.onboardingData()), markOnboardingCompleted: true };
    this.campaignService.update(id, input).subscribe({
      next: () => {
        this.creatingCampaign.set(false);
        this.currentStep.update(s => s + 1); // → step 8: plan approval
        window.scrollTo({ top: 0 });
      },
      error: err => {
        this.creatingCampaign.set(false);
        this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر حفظ بيانات الحملة. حاول مرة أخرى.'), { variant: 'error' });
      },
    });
  }

  /** The plan-approval step now does its own real API calls (research/diagnose/strategy/approve)
   *  — by the time it emits `approve`, the campaign is already stamped PlanApprovedAt server-side,
   *  so this just routes on to the per-post content review page. `autogenerate=1` tells that page
   *  to kick off content generation itself once (see CampaignContentPage), so the user lands on a
   *  page that's already generating rather than one more manual button to press. */
  protected approvePlan(): void {
    localStorage.removeItem(this.stepStorageKey);
    localStorage.removeItem(this.draftIdKey);
    const id = this.campaignId();
    void this.router.navigate(
      id ? ['/dashboard/campaigns', id, 'content'] : ['/dashboard/campaigns'],
      id ? { queryParams: { autogenerate: 1 } } : undefined,
    );
  }

  private buildCreateCampaignInput(brandProfileId: string): CreateCampaignInput {
    const data = this.onboardingData();
    return { brandProfileId, ...this.resolveCampaignFields(data) };
  }

  private buildUpdateCampaignInput(data: OnboardingData): UpdateCampaignInput {
    return this.resolveCampaignFields(data);
  }

  private resolveCampaignFields(data: OnboardingData) {
    const name = (data.campaignName?.trim())
      || (data.brandName ? `حملة ${data.brandName}` : 'حملة جديدة');
    const objective = data.campaignGoal ?? data.campaignOutcome;
    const targetPlatforms = this.resolvePlatforms(data);
    const startDate = data.campaignStartDate || undefined;
    const endDate = this.resolveEndDate(startDate, data.campaignDuration);
    const budgetAmount = this.resolveBudget(data.monthlyBudget);

    return {
      name,
      objective,
      targetPlatforms,
      startDate,
      endDate,
      budgetAmount,
      budgetCurrency: budgetAmount ? 'SAR' : undefined,
      briefJson: JSON.stringify(data),
    };
  }

  private resolvePlatforms(data: OnboardingData): string[] {
    const candidates = [...(data.platformRanking ?? []), ...(data.audiencePlatforms ?? [])];
    const known = candidates.filter(p => BACKEND_PLATFORMS.has(p.toLowerCase()));
    return known.length > 0 ? [...new Set(known)] : ['instagram', 'facebook'];
  }

  private resolveEndDate(startDate: string | undefined, duration: string | undefined): string | undefined {
    if (!startDate) return undefined;
    const months = duration?.match(/(\d+)/)?.[0];
    const start = new Date(startDate);
    if (isNaN(start.getTime())) return undefined;
    start.setMonth(start.getMonth() + (months ? parseInt(months, 10) : 3));
    return start.toISOString().slice(0, 10);
  }

  private resolveBudget(budget: string | undefined): number | undefined {
    if (!budget || budget.includes('لا ميزانية')) return undefined;
    const nums = budget.match(/[\d,]+/g)?.map(n => parseInt(n.replace(/,/g, ''), 10)) ?? [];
    if (nums.length >= 2) return Math.round((nums[0] + nums[1]) / 2);
    if (nums.length === 1) return nums[0];
    return undefined;
  }

  protected updateOnboardingData(partial: Partial<OnboardingData>): void {
    this.onboardingData.update((data) => ({ ...data, ...partial }));
  }

  private loadSavedStep(): number {
    try {
      const raw = localStorage.getItem(this.stepStorageKey);
      const n = parseInt(raw ?? '1', 10);
      return isNaN(n) ? 1 : Math.max(1, Math.min(n, this.totalSteps));
    } catch {
      return 1;
    }
  }
}

type SocialConn = { connected: boolean; accountName?: string };

type OnboardingData = {
  // Step 1 — Campaign Brief
  campaignType?: string;
  campaignName?: string;
  campaignGoal?: string;
  campaignStartDate?: string;
  campaignDuration?: string;
  // Step 1 — New Business Launch
  businessEstablishDate?: string;
  brandIdentityReady?: string;
  businessLaunchDate?: string;
  // Step 1 — New Product Launch
  productName?: string;
  productCategory?: string;
  productAvailability?: string;
  productPricePoint?: string;
  // Step 1 — Drive Sales
  salesScope?: string;
  hasOffer?: string;
  offerDetails?: string;
  salesPeriod?: string;
  // Step 1 — Seasonal Campaign
  occasion?: string;
  seasonStart?: string;
  seasonEnd?: string;
  // Step 1 — Generate Leads
  leadAction?: string;
  hasLandingPage?: string;
  landingPageUrl?: string;
  brandStatusForLeads?: string;
  contentFeeling?: string;
  // Step 1 — Brand Awareness
  mainMessage?: string;
  // Step 1 — Other
  campaignDescription?: string;
  // Step 3 — Brand Overview
  brandName?: string;
  tagline?: string;
  instagram?: string;
  website?: string;
  sector?: string;
  location?: string;
  businessAge?: string;
  stage?: string;
  brandWord1?: string;
  brandWord2?: string;
  brandWord3?: string;
  brandTone?: string[];
  hasGuidelines?: string;
  guidelinesFile?: string;
  brandColors?: string[];
  logoFile?: string;
  languages?: string[];
  productDesc?: string;
  uniqueValue?: string;
  pricePositioning?: string;
  storePresence?: string;
  existingPlatforms?: string[];
  // Step 4 — Target Audience
  gender?: 'female' | 'male' | 'all';
  customerType?: string;
  ageRanges?: string[];
  incomeLevel?: string[];
  customerLocation?: string;
  educationLevel?: string;
  targetDescription?: string;
  interests?: string[];
  painPoints?: string;
  buyingBehavior?: string[];
  hasExistingCustomers?: string;
  audiencePlatforms?: string[];
  // Step 5 — Positioning & Strategy
  positioningVs?: string;
  campaignOutcome?: string;
  successMetrics?: string[];
  brandAdmire1?: string;
  brandAdmire2?: string;
  brandAdmire3?: string;
  monthlyBudget?: string;
  platformRanking?: string[];
  goals?: string[];
  budgetFrom?: number;
  budgetTo?: number;
  targetSales?: string;
  timeframe?: string;
  platforms?: string[];
  agencyExperience?: string;
  socialConnections?: { facebook?: SocialConn; instagram?: SocialConn };
  // Step 6 — Campaign Assets
  campaignPhotos?: string[];
  hashtags?: string;
  additionalNotes?: string;
  // Step 6 (old Identity fields — kept for type safety)
  personality?: string[];
  inspiration?: string;
  identityFiles?: string[];
  referenceLink?: string;
  avoidText?: string;
  strategistAnswers?: Array<{ question: string; answer: string }>;
};
