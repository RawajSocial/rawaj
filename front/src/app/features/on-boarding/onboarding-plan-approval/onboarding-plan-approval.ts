import { Component, computed, inject, input, OnInit, output, signal } from '@angular/core';
import { CampaignService } from '../../../services/campaign.service';
import { CoinPricingService } from '../../../services/coin-pricing.service';
import { ErrorModalService } from '../../../services/error-modal.service';
import { CelebrationModalService } from '../../../services/celebration-modal.service';
import { extractApiErrorMessage } from '../../../core/auth/api-error.util';
import { CoinCostHint } from '../../../shared/components/coin-cost-hint/coin-cost-hint';
import { PermissionService } from '../../../core/tenant/permission.service';
import { TooltipDirective } from '../../../shared/directives/tooltip.directive';
import { GsapRevealDirective } from '../../../shared/directives/gsap-reveal.directive';
import {
  BusinessDiagnosis, CampaignStrategy, CompetitorResearch,
} from '../../../model/campaign.model';

// ──── Platform metadata — used for recommendedPlatforms, audiencePlatforms and existingPlatforms,
// all of which reference the same slug set the onboarding wizard collects. ──
const PLATFORM_META: Record<string, { icon: string; color: string; label: string }> = {
  instagram: { icon: 'fa-brands fa-instagram',  color: 'var(--color-instagram)', label: 'Instagram'   },
  facebook:  { icon: 'fa-brands fa-facebook-f', color: 'var(--color-facebook)',  label: 'Facebook'    },
  tiktok:    { icon: 'fa-brands fa-tiktok',     color: 'var(--color-tiktok)',    label: 'TikTok'      },
  snapchat:  { icon: 'fa-brands fa-snapchat',   color: 'var(--color-snapchat)',  label: 'Snapchat'    },
  twitter:   { icon: 'fa-brands fa-x-twitter',  color: 'var(--color-x)',         label: 'X / Twitter' },
  youtube:   { icon: 'fa-brands fa-youtube',    color: 'var(--color-youtube)',   label: 'YouTube'     },
  linkedin:  { icon: 'fa-brands fa-linkedin',   color: 'var(--color-linkedin)',  label: 'LinkedIn'    },
  whatsapp:  { icon: 'fa-brands fa-whatsapp',   color: 'var(--color-whatsapp)',  label: 'WhatsApp'    },
};

const CAMPAIGN_TYPE_LABELS: Partial<Record<string, string>> = {
  'new-business': 'إطلاق نشاط جديد', 'new-product': 'إطلاق منتج جديد', 'drive-sales': 'زيادة المبيعات',
  seasonal: 'حملة موسمية', leads: 'توليد عملاء', awareness: 'الوعي بالعلامة', other: 'أخرى',
};
const GENDER_LABELS: Partial<Record<string, string>> = { female: 'نساء', male: 'رجال', all: 'الجنسين' };
const CUSTOMER_TYPE_LABELS: Partial<Record<string, string>> = { b2c: 'أفراد (B2C)', b2b: 'شركات (B2B)', both: 'أفراد وشركات' };
const CUSTOMER_LOCATION_LABELS: Partial<Record<string, string>> = { local: 'محلي (مدينة واحدة)', national: 'وطني (داخل الدولة)', international: 'دولي' };
const INCOME_LEVEL_LABELS: Partial<Record<string, string>> = { budget: 'اقتصادي', mid: 'متوسط', high: 'مرتفع', luxury: 'فاخر جداً', business: 'أصحاب أعمال' };
const BUYING_BEHAVIOR_LABELS: Partial<Record<string, string>> = {
  impulse: 'مشتري اندفاعي', research: 'يبحث كثيراً', loyal: 'وفي للعلامة',
  deals: 'يبحث عن العروض', social: 'متأثر برأي الآخرين', quality: 'يهتم بالجودة أولاً',
};
const SUCCESS_METRIC_LABELS: Partial<Record<string, string>> = {
  followers: 'متابعون أكثر', visits: 'زيارات الموقع', sales: 'مبيعات أكثر', leads: 'عملاء محتملون',
  downloads: 'تحميلات أكثر', enquiries: 'استفسارات أكثر', awareness: 'وعي بالعلامة',
};
const POSITIONING_LABELS: Partial<Record<string, string>> = {
  affordable: 'أرخص من المنافسين', premium: 'أكثر راقياً وجودةً', consistent: 'أكثر ثباتاً واتساقاً',
  reliable: 'أكثر موثوقيةً وثقةً', personal: 'أكثر شخصيةً ومحليةً', sustainable: 'أكثر استدامةً', convenient: 'أكثر سهولةً وراحةً',
};
const LANGUAGE_LABELS: Partial<Record<string, string>> = { ar: 'العربية', en: 'الإنجليزية', 'ar-en': 'العربية والإنجليزية' };
const PRICE_POSITIONING_LABELS: Partial<Record<string, string>> = { budget: 'اقتصادي', mid: 'متوسط', premium: 'راقٍ', luxury: 'فاخر' };

@Component({
  selector: 'app-onboarding-plan-approval',
  imports: [CoinCostHint, TooltipDirective, GsapRevealDirective],
  templateUrl: './onboarding-plan-approval.html',
  styleUrl: './onboarding-plan-approval.css',
})
export class OnboardingPlanApproval implements OnInit {
  readonly data       = input<ApprovalOnboardingData | null>(null);
  readonly campaignId = input<string | null>(null);
  /** Label for the "back" action — the onboarding wizard goes back a step ("تعديل البيانات"),
   *  while a standalone review of an already-created campaign just goes back ("رجوع"). */
  readonly backLabel  = input('تعديل البيانات');
  /** Whether a campaign with no strategy yet should start the (paid) research → diagnosis →
   *  strategy pipeline on its own. True inside the onboarding wizard, where reaching this step IS
   *  the user asking for a plan. False everywhere else: CampaignStrategyPage is reached from a
   *  button labelled "review the strategy", and auto-running there charged the tenant thousands of
   *  coins for merely opening a page — so it asks first (see `startPipeline`). */
  readonly autoGenerate = input(true);
  readonly approve    = output<void>();
  readonly back       = output<void>();

  private readonly campaignService = inject(CampaignService);
  private readonly coinPricingService = inject(CoinPricingService);
  private readonly errorModalService = inject(ErrorModalService);
  private readonly celebrationModalService = inject(CelebrationModalService);
  protected readonly perms = inject(PermissionService);

  // ── Real AI pipeline state (research → diagnosis → strategy) — this IS the plan; there is no
  // separate locally-fabricated "plan" model here. Each stage's `*Done` flag flips independently
  // (in both the success and error path of its call) so every section of the review below can
  // reveal itself the moment its own data is ready, instead of the whole page waiting on the
  // slowest of the three calls. ──
  protected readonly competitorResearch = signal<CompetitorResearch | null>(null);
  protected readonly diagnosis          = signal<BusinessDiagnosis | null>(null);
  protected readonly strategy           = signal<CampaignStrategy | null>(null);
  protected readonly pipelineError      = signal<string | null>(null);

  /** Whether the campaign has a strategy stored server-side, which is a different question from
   *  whether `strategy()` parsed. `ApproveCampaignPlanCommandHandler` gates approval on
   *  `AiPlanJson` being non-empty, so this is the signal the approve button must follow — gating
   *  it on the parsed object instead left the user unable to approve a plan the backend was
   *  perfectly willing to accept, with no way forward. (The backend now stores only parseable
   *  JSON, so the two agree for new campaigns; this still matters for rows written before that.) */
  protected readonly hasServerPlan      = signal(false);

  protected readonly researchDone  = signal(false);
  protected readonly diagnosisDone = signal(false);
  protected readonly strategyDone  = signal(false);
  /** True once nothing more will change without a manual refine — used to hide the inline
   *  "AI still working" progress strip at the top of the review. */
  protected readonly pipelineFinished = computed(() => this.researchDone() && this.diagnosisDone() && this.strategyDone());

  protected readonly pipelineStages = [
    { key: 'research' as const,  label: 'البحث عن المنافسين' },
    { key: 'diagnosis' as const, label: 'تحليل ما فهمناه عن نشاطك' },
    { key: 'strategy' as const,  label: 'بناء استراتيجية الحملة الكاملة' },
  ];

  /** Set when this campaign has no strategy yet and `autoGenerate` is off — the review sections
   *  below have nothing to show, so the page offers to generate one (with its price) instead of
   *  either spending coins unasked or sitting on an empty page. */
  protected readonly awaitingGenerateConsent = signal(false);

  protected readonly refineFeedback = signal('');
  protected readonly refining       = signal(false);
  protected readonly approving      = signal(false);
  protected readonly approved       = signal(false);

  protected readonly platformMeta = PLATFORM_META;
  protected readonly pricing = this.coinPricingService.pricing;

  /** What the full pipeline actually costs, so the "generate my strategy" prompt states a real
   *  number instead of asking the user to approve an unknown spend. Competitor research is only
   *  charged when it finds something, so it's the one part this can't promise. */
  protected readonly pipelineCost = computed(() => {
    const p = this.pricing();
    if (!p) return null;
    return p.discountedCosts.businessDiagnosis + p.discountedCosts.marketingPlanGeneration;
  });

  // ── Section presence — drives whether a card shows real content or its empty state once its
  // data has actually finished loading (see pipelineFinished/*Done above for the loading state). ──
  protected readonly hasBusinessUnderstanding = computed(() => {
    const d = this.diagnosis();
    const raw = this.data();
    return !!(d?.businessSummary || d?.swot || d?.businessMaturity || d?.growthStage || raw?.sector || raw?.productDesc || raw?.uniqueValue);
  });

  protected readonly hasObjectiveData = computed(() => {
    const d = this.data();
    return !!(d?.campaignType || d?.campaignGoal || d?.campaignOutcome || d?.positioningVs || d?.monthlyBudget || d?.campaignStartDate);
  });

  protected readonly hasAudienceData = computed(() => {
    const d = this.data();
    return !!(d?.gender || d?.customerType || d?.ageRanges?.length || d?.interests?.length || d?.customerLocation
      || d?.incomeLevel?.length || d?.educationLevel || d?.painPoints || d?.buyingBehavior?.length
      || d?.targetDescription || d?.audiencePlatforms?.length);
  });

  protected readonly hasBrandVoiceData = computed(() => {
    const d = this.data();
    return !!(d?.brandWord1 || d?.brandWord2 || d?.brandWord3 || d?.brandTone?.length
      || d?.tagline || d?.brandColors?.length || d?.languages?.length);
  });

  protected readonly hasMarketingApproach = computed(() => {
    const s = this.strategy();
    return !!(s?.marketingStrategy || s?.brandStrategy || s?.campaignBlueprint);
  });

  protected readonly hasExpectedOutcome = computed(() =>
    !!(this.data()?.successMetrics?.length || this.strategy()?.executionRoadmap || this.strategy()?.contentProductionPlan),
  );

  protected readonly hasAiAnalysis = computed(() => {
    const cr = this.competitorResearch();
    const d = this.diagnosis();
    const s = this.strategy();
    return !!(
      (cr && !cr.unavailable) || d?.currentRisks?.length || d?.opportunities?.length
      || s?.aiRecommendations?.length || s?.executiveSummary || s?.businessAndMarketAnalysis
    );
  });

  protected readonly campaignTypeLabels = CAMPAIGN_TYPE_LABELS;
  protected readonly genderLabels = GENDER_LABELS;
  protected readonly customerTypeLabels = CUSTOMER_TYPE_LABELS;
  protected readonly customerLocationLabels = CUSTOMER_LOCATION_LABELS;
  protected readonly incomeLevelLabels = INCOME_LEVEL_LABELS;
  protected readonly buyingBehaviorLabels = BUYING_BEHAVIOR_LABELS;
  protected readonly successMetricLabels = SUCCESS_METRIC_LABELS;
  protected readonly positioningLabels = POSITIONING_LABELS;
  protected readonly languageLabels = LANGUAGE_LABELS;
  protected readonly pricePositioningLabels = PRICE_POSITIONING_LABELS;

  constructor() {
    this.coinPricingService.ensureLoaded();
  }

  ngOnInit(): void {
    const campaignId = this.campaignId();
    if (!campaignId) {
      // No real campaign to run the pipeline against (shouldn't happen from the wizard, but keep
      // the page usable rather than stuck on a loader forever).
      this.pipelineError.set('تعذّر العثور على الحملة. عد للخطوة السابقة وحاول مرة أخرى.');
      this.researchDone.set(true);
      this.diagnosisDone.set(true);
      this.strategyDone.set(true);
      return;
    }

    // This component is also reused outside the wizard (see CampaignStrategyPage) to let a user
    // come back to a draft campaign's strategy later. Re-running research/diagnosis/generate-plan
    // on every visit would re-spend coins for a strategy that's already there, so check for one
    // first and only run the (paid) pipeline when nothing has been generated yet.
    this.campaignService.getCampaign(campaignId).subscribe({
      next: res => {
        const existingPlan = res.data?.aiPlanJson;
        if (existingPlan) {
          this.hasServerPlan.set(true);
          this.competitorResearch.set(this.parseJson<CompetitorResearch>(res.data?.competitorResearchJson));
          this.diagnosis.set(this.parseJson<BusinessDiagnosis>(res.data?.diagnosisJson));
          this.strategy.set(this.parseJson<CampaignStrategy>(existingPlan));
          this.approved.set(!!res.data?.planApprovedAt);
          this.researchDone.set(true);
          this.diagnosisDone.set(true);
          this.strategyDone.set(true);
        } else {
          this.startOrOfferPipeline(campaignId);
        }
      },
      // The campaign couldn't be read, so whether it already has a (paid-for) strategy is unknown
      // — never spend coins re-generating one on a guess. Ask, or let the wizard's own flow
      // (autoGenerate) proceed as before.
      error: () => this.startOrOfferPipeline(campaignId),
    });
  }

  private startOrOfferPipeline(campaignId: string): void {
    if (!this.autoGenerate() || !this.perms.canEdit()) {
      this.awaitingGenerateConsent.set(true);
      // Nothing is in flight, so the "AI still working" strip must not claim otherwise.
      this.researchDone.set(true);
      this.diagnosisDone.set(true);
      this.strategyDone.set(true);
      return;
    }
    this.runPipeline(campaignId);
  }

  /** The explicit "generate my strategy" action behind `awaitingGenerateConsent`. */
  protected startPipeline(): void {
    const campaignId = this.campaignId();
    if (!campaignId || !this.awaitingGenerateConsent() || !this.perms.canEdit()) return;
    this.awaitingGenerateConsent.set(false);
    this.researchDone.set(false);
    this.diagnosisDone.set(false);
    this.strategyDone.set(false);
    this.hasServerPlan.set(false);
    this.pipelineError.set(null);
    this.runPipeline(campaignId);
  }

  /** True once the pipeline has finished but produced no usable strategy — the model call failed,
   *  or returned something that wasn't parseable as the expected JSON. Distinct from
   *  `awaitingGenerateConsent` (nothing was ever attempted) because this state has already spent
   *  the research/diagnosis steps and only the last one needs re-running. */
  protected readonly strategyFailed = computed(() =>
    this.pipelineFinished() && !this.hasServerPlan() && !this.awaitingGenerateConsent(),
  );

  /** Why the approve button is disabled, for its tooltip. A disabled primary CTA with no
   *  explanation is indistinguishable from a broken page — which is exactly how the
   *  strategy-failed state read before the recovery card above existed. */
  protected readonly approveDisabledReason = computed(() => {
    if (!this.perms.canEdit()) return this.perms.editDeniedReason();
    if (this.approved()) return 'تم اعتماد هذه الخطة بالفعل.';
    if (this.awaitingGenerateConsent()) return 'ولّد الاستراتيجية أولاً ثم اعتمدها.';
    if (!this.hasServerPlan()) return 'يجب أن تكتمل الاستراتيجية قبل اعتمادها — أعد المحاولة من الرسالة أعلاه.';
    return '';
  });

  /** Re-runs only the strategy step after a failure. Research and diagnosis already succeeded and
   *  were already paid for, so re-running the whole pipeline would charge for them twice.
   *  `GenerateMarketingPlanCommandHandler` charges only after a successful generation (it returns
   *  before `CoinPolicy.TrySpendAsync` when the model call fails), so the failed attempt cost
   *  nothing and this retry is not a double-charge. */
  protected retryStrategy(): void {
    const campaignId = this.campaignId();
    if (!campaignId || !this.strategyFailed() || !this.perms.canEdit()) return;
    this.pipelineError.set(null);
    this.strategyDone.set(false);
    this.runStrategy(campaignId);
  }

  /** Runs the real research → diagnosis → strategy pipeline once, in order, against the campaign
   *  the wizard just created. Competitor research is best-effort and never blocks progress — a
   *  Tavily failure just means the "unavailable" note renders instead of competitor cards. Every
   *  charged step refreshes the coin balance so the header chip never lags behind the spend. */
  private runPipeline(campaignId: string): void {
    this.campaignService.researchCompetitors(campaignId).subscribe({
      next: res => {
        if (res.data) {
          this.competitorResearch.set(this.parseJson<CompetitorResearch>(res.data.competitorResearchJson));
        }
        this.researchDone.set(true);
        this.coinPricingService.refreshAfterSpend();
        this.runDiagnosis(campaignId);
      },
      error: () => {
        this.researchDone.set(true);
        this.coinPricingService.refreshAfterSpend();
        this.runDiagnosis(campaignId); // best-effort — proceed regardless
      },
    });
  }

  private runDiagnosis(campaignId: string): void {
    this.campaignService.diagnoseBusiness(campaignId).subscribe({
      next: res => {
        if (res.data) this.diagnosis.set(this.parseJson<BusinessDiagnosis>(res.data.diagnosisJson));
        this.diagnosisDone.set(true);
        this.coinPricingService.refreshAfterSpend();
        this.runStrategy(campaignId);
      },
      error: err => {
        this.pipelineError.set(extractApiErrorMessage(err, 'تعذّر إعداد تحليل النشاط.'));
        this.diagnosisDone.set(true);
        this.coinPricingService.refreshAfterSpend();
        this.runStrategy(campaignId); // still attempt the strategy — diagnosis is advisory input to it
      },
    });
  }

  private runStrategy(campaignId: string): void {
    this.campaignService.generatePlan(campaignId).subscribe({
      next: res => {
        if (res.data?.aiPlanJson) this.hasServerPlan.set(true);
        const parsed = res.data ? this.parseJson<CampaignStrategy>(res.data.aiPlanJson) : null;
        this.strategy.set(parsed);
        // A 200 whose body doesn't parse into the expected shape is still a failed generation as
        // far as the user is concerned — without this it looked like a success that silently
        // rendered nothing. (The backend now rejects unparseable output rather than storing it,
        // so this should no longer be reachable for new generations; kept as a safety net.)
        if (!parsed) {
          this.pipelineError.set('تعذّر قراءة الاستراتيجية التي وصلت من الذكاء الاصطناعي. أعد المحاولة.');
        }
        this.strategyDone.set(true);
        this.coinPricingService.refreshAfterSpend();
      },
      error: err => {
        // The old message promised the user could "try again or continue with the initial plan" —
        // neither existed: there was no retry control, and there is no initial plan to continue
        // with. The recovery card in the template now provides the retry this refers to.
        this.pipelineError.set(extractApiErrorMessage(err, 'تعذّر توليد الاستراتيجية.'));
        this.strategyDone.set(true);
        this.coinPricingService.refreshAfterSpend();
      },
    });
  }

  /**
   * Parses an AI-produced JSON blob, tolerating a markdown fence or a line of commentary wrapped
   * around it (```json { … } ```), which models add despite being told not to.
   *
   * The backend now strips this before storing (see `AiJsonResponseParser`), so new generations
   * arrive clean — but campaigns generated before that fix still hold the wrapped text, and their
   * whole strategy review rendered blank because a plain `JSON.parse` threw on the fence. Reading
   * those correctly costs one substring and makes existing campaigns work again rather than
   * requiring the user to pay to regenerate.
   */
  private parseJson<T>(raw: string | null | undefined): T | null {
    if (!raw) return null;

    const text = raw.trim();
    try { return JSON.parse(text) as T; } catch { /* fall through to the unwrapping attempt */ }

    const objectStart = text.indexOf('{');
    const arrayStart = text.indexOf('[');
    const start = objectStart < 0 ? arrayStart
      : arrayStart < 0 ? objectStart
      : Math.min(objectStart, arrayStart);
    if (start < 0) return null;

    const end = text.lastIndexOf(text[start] === '{' ? '}' : ']');
    if (end <= start) return null;

    try { return JSON.parse(text.slice(start, end + 1)) as T; } catch { return null; }
  }

  /** "عدّل الخطة" — free-text refinement of the just-generated strategy (AI Reasoning Conversation). */
  protected submitRefine(): void {
    const campaignId = this.campaignId();
    const feedback = this.refineFeedback().trim();
    if (!campaignId || !feedback || this.refining() || !this.perms.canEdit()) return;

    this.refining.set(true);
    this.campaignService.refinePlan(campaignId, feedback).subscribe({
      next: res => {
        this.refining.set(false);
        this.coinPricingService.refreshAfterSpend();
        if (res.data) {
          this.strategy.set(this.parseJson<CampaignStrategy>(res.data.aiPlanJson));
          this.refineFeedback.set('');
        }
      },
      error: err => {
        this.refining.set(false);
        this.coinPricingService.refreshAfterSpend();
        this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر تعديل الخطة.'), { variant: 'error' });
      },
    });
  }

  protected updateRefineFeedback(value: string): void {
    this.refineFeedback.set(value);
  }

  protected platformLabel(key: string): string {
    return PLATFORM_META[key.toLowerCase()]?.label ?? key;
  }

  protected platformIcon(key: string): string {
    return PLATFORM_META[key.toLowerCase()]?.icon ?? 'fa-solid fa-hashtag';
  }

  protected platformColor(key: string): string {
    return PLATFORM_META[key.toLowerCase()]?.color ?? '#6b7280';
  }

  protected objectEntries<T>(obj: Record<string, T> | undefined): [string, T][] {
    return obj ? Object.entries(obj) : [];
  }

  onApprove(): void {
    const campaignId = this.campaignId();
    if (!campaignId || this.approving() || !this.perms.canEdit()) return;

    this.approving.set(true);
    this.campaignService.approvePlan(campaignId).subscribe({
      next: () => {
        this.approving.set(false);
        this.approved.set(true);
        this.celebrationModalService.show('اعتمدت استراتيجية حملتك — ننتقل الآن لتوليد المحتوى.', 'الخطة جاهزة!');
        this.approve.emit();
      },
      error: err => {
        this.approving.set(false);
        this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر اعتماد الخطة.'), { variant: 'error' });
      },
    });
  }

  onBack(): void { this.back.emit(); }
}

// ──── Types ────────────────────────────────────────────────────────────────
/** Mirrors (a superset-compatible subset of) the onboarding wizard's local `OnboardingData` shape
 *  — kept as its own type here (rather than importing RawajOnboarding's) since the two components
 *  already don't share a type today and the campaign-strategy-page resume path builds this same
 *  shape by parsing the campaign's persisted `briefJson` instead of holding wizard state. */
export type ApprovalOnboardingData = {
  brandName?: string;
  sector?: string;
  productDesc?: string;
  uniqueValue?: string;
  businessAge?: string;
  stage?: string;
  pricePositioning?: string;
  campaignType?: string;
  campaignDuration?: string;
  campaignGoal?: string;
  campaignOutcome?: string;
  campaignStartDate?: string;
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
  audiencePlatforms?: string[];
  positioningVs?: string;
  successMetrics?: string[];
  monthlyBudget?: string;
  platformRanking?: string[];
  brandWord1?: string;
  brandWord2?: string;
  brandWord3?: string;
  brandTone?: string[];
  tagline?: string;
  brandColors?: string[];
  languages?: string[];
};
