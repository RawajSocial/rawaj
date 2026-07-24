import { Component, inject, input, OnInit, output, signal } from '@angular/core';
import { CampaignService } from '../../../services/campaign.service';
import { CoinPricingService } from '../../../services/coin-pricing.service';
import { ErrorModalService } from '../../../services/error-modal.service';
import { extractApiErrorMessage } from '../../../core/auth/api-error.util';
import { CoinCostHint } from '../../../shared/components/coin-cost-hint/coin-cost-hint';
import { PermissionService } from '../../../core/tenant/permission.service';
import { TooltipDirective } from '../../../shared/directives/tooltip.directive';
import {
  BusinessDiagnosis, CampaignStrategy, CompetitorResearch,
} from '../../../model/campaign.model';

// ──── Platform metadata (still used to render CampaignStrategy.campaignBlueprint.recommendedPlatforms) ──
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

@Component({
  selector: 'app-onboarding-plan-approval',
  imports: [CoinCostHint, TooltipDirective],
  templateUrl: './onboarding-plan-approval.html',
  styleUrl: './onboarding-plan-approval.css',
})
export class OnboardingPlanApproval implements OnInit {
  readonly data       = input<ApprovalOnboardingData | null>(null);
  readonly campaignId = input<string | null>(null);
  readonly approve    = output<void>();
  readonly back       = output<void>();

  private readonly campaignService = inject(CampaignService);
  private readonly coinPricingService = inject(CoinPricingService);
  private readonly errorModalService = inject(ErrorModalService);
  protected readonly perms = inject(PermissionService);

  protected readonly phase       = signal<'loading' | 'plan'>('loading');
  protected readonly loadingStep = signal(0);

  protected readonly loadingMessages = [
    'البحث عن المنافسين في نفس نشاطك...',
    'تحليل ما فهمناه عن نشاطك...',
    'بناء استراتيجية الحملة الكاملة...',
  ];

  // ── Real AI pipeline state (research → diagnosis → strategy) — this IS the plan; there is no
  // separate locally-fabricated "plan" model here, unlike the previous version of this page. ──
  protected readonly competitorResearch = signal<CompetitorResearch | null>(null);
  protected readonly diagnosis          = signal<BusinessDiagnosis | null>(null);
  protected readonly strategy           = signal<CampaignStrategy | null>(null);
  protected readonly pipelineError      = signal<string | null>(null);

  protected readonly refineFeedback = signal('');
  protected readonly refining       = signal(false);
  protected readonly approving      = signal(false);
  protected readonly approved       = signal(false);

  protected readonly platformMeta = PLATFORM_META;
  protected readonly pricing = this.coinPricingService.pricing;

  constructor() {
    this.coinPricingService.ensureLoaded();
  }

  ngOnInit(): void {
    const campaignId = this.campaignId();
    if (!campaignId) {
      // No real campaign to run the pipeline against (shouldn't happen from the wizard, but keep
      // the page usable rather than stuck on a loader forever).
      this.pipelineError.set('تعذّر العثور على الحملة. عد للخطوة السابقة وحاول مرة أخرى.');
      this.phase.set('plan');
      return;
    }
    this.runPipeline(campaignId);
  }

  /** Runs the real research → diagnosis → strategy pipeline once, in order, against the campaign
   *  the wizard just created. Competitor research is best-effort and never blocks progress — a
   *  Tavily failure just means the "unavailable" note renders instead of competitor cards. Every
   *  charged step refreshes the coin balance so the header chip never lags behind the spend. */
  private runPipeline(campaignId: string): void {
    this.loadingStep.set(0);
    this.campaignService.researchCompetitors(campaignId).subscribe({
      next: res => {
        if (res.data) {
          this.competitorResearch.set(this.parseJson<CompetitorResearch>(res.data.competitorResearchJson));
        }
        this.coinPricingService.refreshAfterSpend();
        this.runDiagnosis(campaignId);
      },
      error: () => {
        this.coinPricingService.refreshAfterSpend();
        this.runDiagnosis(campaignId); // best-effort — proceed regardless
      },
    });
  }

  private runDiagnosis(campaignId: string): void {
    this.loadingStep.set(1);
    this.campaignService.diagnoseBusiness(campaignId).subscribe({
      next: res => {
        if (res.data) this.diagnosis.set(this.parseJson<BusinessDiagnosis>(res.data.diagnosisJson));
        this.coinPricingService.refreshAfterSpend();
        this.runStrategy(campaignId);
      },
      error: err => {
        this.pipelineError.set(extractApiErrorMessage(err, 'تعذّر إعداد تحليل النشاط.'));
        this.coinPricingService.refreshAfterSpend();
        this.runStrategy(campaignId); // still attempt the strategy — diagnosis is advisory input to it
      },
    });
  }

  private runStrategy(campaignId: string): void {
    this.loadingStep.set(2);
    this.campaignService.generatePlan(campaignId).subscribe({
      next: res => {
        if (res.data) this.strategy.set(this.parseJson<CampaignStrategy>(res.data.aiPlanJson));
        this.coinPricingService.refreshAfterSpend();
        this.phase.set('plan');
      },
      error: err => {
        this.pipelineError.set(extractApiErrorMessage(err, 'تعذّر توليد الاستراتيجية. يمكنك المحاولة مجدداً أو المتابعة بالخطة الأولية.'));
        this.coinPricingService.refreshAfterSpend();
        this.phase.set('plan');
      },
    });
  }

  private parseJson<T>(raw: string | null | undefined): T | null {
    if (!raw) return null;
    try { return JSON.parse(raw) as T; } catch { return null; }
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
export type ApprovalOnboardingData = {
  brandName?: string;
  sector?: string;
  campaignType?: string;
  campaignDuration?: string;
  campaignOutcome?: string;
  gender?: 'female' | 'male' | 'all';
  audiencePlatforms?: string[];
  successMetrics?: string[];
  positioningVs?: string;
  monthlyBudget?: string;
  platformRanking?: string[];
};
