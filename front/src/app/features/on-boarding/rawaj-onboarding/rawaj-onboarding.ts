import { Component, computed, effect, inject, signal, WritableSignal } from '@angular/core';
import { Router } from '@angular/router';
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
import { extractApiErrorMessage } from '../../../core/auth/api-error.util';
import { CreateCampaignInput } from '../../../model/campaign.model';

/** Frontend platform slugs the backend's SocialPlatform enum actually accepts (case-insensitively).
 *  Anything else the wizard collects (e.g. snapchat, whatsapp) has no campaign-level equivalent yet
 *  and is dropped rather than sent, since CreateCampaignCommandHandler throws on an unknown name. */
const BACKEND_PLATFORMS = new Set(['instagram', 'facebook', 'tiktok', 'twitter', 'youtube', 'linkedin']);

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
  private readonly storageKey = 'rawaj.onboarding.data';
  private readonly stepStorageKey = 'rawaj.onboarding.step';
  private readonly seo = inject(SeoService);
  private readonly router = inject(Router);
  private readonly brandProfileService = inject(BrandProfileService);
  private readonly brandContextService = inject(BrandContextService);
  private readonly campaignService = inject(CampaignService);
  private readonly errorModalService = inject(ErrorModalService);

  /** The brand profile this campaign will be assigned to — defaults to whichever the header's
   *  global brand switcher already has selected, but the sidebar lets the user pick a different
   *  one of their brands when they have more than one. */
  protected readonly brandProfiles = this.brandProfileService.profiles;
  protected readonly selectedBrandProfileId = signal<string | null>(null);
  protected readonly creatingCampaign = signal(false);
  protected readonly campaignId = signal<string | null>(null);

  protected readonly selectedBrandProfile = computed(() =>
    this.brandProfiles().find(p => p.id === this.selectedBrandProfileId()),
  );

  constructor() {
    this.currentStep = signal(this.loadSavedStep());
    this.onboardingData.set(this.loadOnboardingData());
    this.seo.setPageSeo({
      title: 'إعداد الحساب | رواج',
      description: 'ابدأ إعداد حسابك في رواج عبر خطوات مخصصة لنوع نشاطك التجاري.',
      keywords: 'رواج, إعداد الحساب, نوع الحساب, التسويق الذكي',
      path: '/on-boarding',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });

    const defaultBrandId = this.brandContextService.selectedBrandProfileId() ?? this.brandProfiles()[0]?.id ?? null;
    this.selectedBrandProfileId.set(defaultBrandId);
    if (this.brandProfiles().length === 0) {
      this.brandProfileService.refresh().subscribe(res => {
        const first = res.data?.[0]?.brandProfileId;
        if (first) this.selectedBrandProfileId.set(first);
        else this.redirectToCreateBrandProfile();
      });
    }

    effect(() => {
      this.saveOnboardingData(this.onboardingData());
    });

    effect(() => {
      const step = this.currentStep();
      if (step >= 1 && step <= this.totalSteps) {
        localStorage.setItem(this.stepStorageKey, String(step));
      }
    });
  }

  protected selectBrandProfile(id: string): void {
    this.selectedBrandProfileId.set(id);
  }

  private redirectToCreateBrandProfile(): void {
    this.errorModalService.show(
      'يجب إنشاء ملف علامة تجارية أولاً لاستخدام هذه الميزة.',
      { variant: 'warning', title: 'يلزم إنشاء ملف علامة تجارية' },
    );
    void this.router.navigate(['/dashboard/brand-profiles/new']);
  }

  protected goToNextStep(): void {
    this.currentStep.update((step) => Math.min(this.totalSteps + 2, step + 1));
    window.scrollTo({ top: 0 });
  }

  protected goToPreviousStep(): void {
    this.currentStep.update((step) => Math.max(1, step - 1));
    window.scrollTo({ top: 0 });
  }

  /** Fires once the step-7 AI chat finishes. Persists everything collected so far as a real
   *  campaign row (BriefJson = the full onboarding blob) before moving into the plan-approval
   *  step, which needs a real campaignId to run its research/diagnosis/strategy pipeline against. */
  protected finishOnboarding(): void {
    const brandProfileId = this.selectedBrandProfileId();
    if (!brandProfileId) {
      this.redirectToCreateBrandProfile();
      return;
    }

    this.creatingCampaign.set(true);
    const input = this.buildCreateCampaignInput(brandProfileId);
    this.campaignService.create(input).subscribe({
      next: res => {
        this.creatingCampaign.set(false);
        if (res.data) {
          this.campaignId.set(res.data.campaignId);
          this.currentStep.update(s => s + 1); // → step 8: plan approval
          window.scrollTo({ top: 0 });
        }
      },
      error: err => {
        this.creatingCampaign.set(false);
        this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر إنشاء الحملة. حاول مرة أخرى.'), { variant: 'error' });
      },
    });
  }

  /** The plan-approval step now does its own real API calls (research/diagnose/strategy/approve)
   *  — by the time it emits `approve`, the campaign is already stamped PlanApprovedAt server-side,
   *  so this just routes on to the per-post content review page. */
  protected approvePlan(): void {
    localStorage.removeItem(this.stepStorageKey);
    const id = this.campaignId();
    void this.router.navigate(id ? ['/dashboard/campaigns', id, 'content'] : ['/dashboard/campaigns']);
  }

  private buildCreateCampaignInput(brandProfileId: string): CreateCampaignInput {
    const data = this.onboardingData();
    const name = (data.campaignName?.trim())
      || (data.brandName ? `حملة ${data.brandName}` : 'حملة جديدة');
    const objective = data.campaignGoal ?? data.campaignOutcome;
    const targetPlatforms = this.resolvePlatforms(data);
    const startDate = data.campaignStartDate || undefined;
    const endDate = this.resolveEndDate(startDate, data.campaignDuration);
    const budgetAmount = this.resolveBudget(data.monthlyBudget);

    return {
      brandProfileId,
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

  private loadOnboardingData(): OnboardingData {
    const raw = localStorage.getItem(this.storageKey);
    if (!raw) return {};
    try {
      return JSON.parse(raw) as OnboardingData;
    } catch {
      return {};
    }
  }

  private saveOnboardingData(data: OnboardingData): void {
    localStorage.setItem(this.storageKey, JSON.stringify(data));
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
