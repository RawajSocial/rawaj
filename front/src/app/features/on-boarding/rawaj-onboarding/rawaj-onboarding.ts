import { Component, effect, inject, signal, WritableSignal } from '@angular/core';
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
import { CampaignsApiService } from '../../../core/api/campaigns-api.service';
import { BrandProfilesApiService } from '../../../core/api/brand-profiles-api.service';
import { TenantService } from '../../../core/tenant/tenant.service';
import { ApiError } from '../../../core/api';
import { BrandVoice, CreateCampaignRequest, UpdateBrandProfileRequest } from '../../../core/models';

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
  protected readonly submitting = signal(false);
  protected readonly submitError = signal<string | null>(null);
  private readonly storageKey = 'rawaj.onboarding.data';
  private readonly stepStorageKey = 'rawaj.onboarding.step';
  private readonly seo = inject(SeoService);
  private readonly router = inject(Router);
  private readonly campaignsApi = inject(CampaignsApiService);
  private readonly brandProfilesApi = inject(BrandProfilesApiService);
  private readonly tenantService = inject(TenantService);

  constructor() {
    this.currentStep = signal(this.loadSavedStep());
    this.onboardingData.set(this.loadOnboardingData());

    // Re-hydrates tenant/brand context on a hard page refresh or direct navigation - loadContext()
    // after login/register only covers the in-app navigation case (mirrors Dashboard's guard).
    if (!this.tenantService.loaded()) {
      this.tenantService.loadContext().subscribe();
    }

    this.seo.setPageSeo({
      title: 'Rawaj Onboarding | Step 1',
      description: 'ابدأ إعداد حسابك في Rawaj عبر خطوات Onboarding مخصصة لنوع نشاطك.',
      keywords: 'Rawaj onboarding, إعداد الحساب, نوع الحساب, التسويق الذكي',
      path: '/rawaj-onboarding',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });

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

  protected goToNextStep(): void {
    this.currentStep.update((step) => Math.min(this.totalSteps + 2, step + 1));
    window.scrollTo({ top: 0 });
  }

  protected goToPreviousStep(): void {
    this.currentStep.update((step) => Math.max(1, step - 1));
    window.scrollTo({ top: 0 });
  }

  protected finishOnboarding(): void {
    this.currentStep.update(s => s + 1); // → step 8: plan approval
  }

  protected approvePlan(): void {
    const brandProfileId = this.tenantService.activeBrandProfile()?.brandProfileId;
    if (!brandProfileId) {
      this.submitError.set('لم يتم العثور على البراند الخاص بك. حاول تسجيل الدخول مجددًا.');
      return;
    }

    const data = this.onboardingData();
    this.submitting.set(true);
    this.submitError.set(null);

    // Account Setup already created a bare-bones brand profile (just a name) so the user could
    // reach the dashboard quickly. This wizard collected much richer brand data since - refine
    // that same profile now. Fire-and-forget: enrichment failing shouldn't block campaign
    // creation, the primary action of this step.
    this.brandProfilesApi.update(brandProfileId, this.buildBrandProfileUpdate(data)).subscribe({ error: () => undefined });

    const request: CreateCampaignRequest = {
      brandProfileId,
      name: data.campaignName?.trim() || (data.brandName ? `حملة ${data.brandName}` : 'حملة جديدة'),
      objective: this.buildObjective(data),
      targetPlatforms: this.mapPlatforms(data.platforms),
      startDate: data.campaignStartDate || null,
      endDate: null,
      budgetAmount: data.budgetFrom ?? null,
      budgetCurrency: data.budgetFrom ? 'USD' : null,
    };

    this.campaignsApi.create(request).subscribe({
      next: (campaign) => {
        // AI plan generation runs after navigation - the marketing plan page shows its own
        // generating/failed state, so a slow or quota-limited AI call shouldn't strand the user
        // on this wizard.
        this.campaignsApi.generatePlan(campaign.campaignId).subscribe({ error: () => undefined });

        localStorage.removeItem(this.stepStorageKey);
        localStorage.removeItem(this.storageKey);
        this.submitting.set(false);
        void this.router.navigate(['/dashboard/marketing-plan']);
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        this.submitError.set(error instanceof ApiError ? error.message : 'تعذر إنشاء الحملة، حاول مرة أخرى.');
      },
    });
  }

  private buildBrandProfileUpdate(data: OnboardingData): UpdateBrandProfileRequest {
    const targetAudienceParts: string[] = [];
    if (data.gender && data.gender !== 'all') targetAudienceParts.push(`الجنس: ${data.gender === 'female' ? 'إناث' : 'ذكور'}`);
    if (data.ageRanges?.length) targetAudienceParts.push(`الفئة العمرية: ${data.ageRanges.join('، ')}`);
    if (data.customerType) targetAudienceParts.push(`نوع العميل: ${data.customerType}`);
    if (data.interests?.length) targetAudienceParts.push(`الاهتمامات: ${data.interests.join('، ')}`);
    if (data.painPoints) targetAudienceParts.push(`نقاط الألم: ${data.painPoints}`);
    if (data.targetDescription) targetAudienceParts.push(data.targetDescription);

    const keywords = [data.brandWord1, data.brandWord2, data.brandWord3].filter((w): w is string => !!w);
    const colors = (data.brandColors ?? []).filter((c) => !!c);

    return {
      name: data.brandName?.trim() || undefined,
      tagline: data.tagline || undefined,
      industry: data.sector || undefined,
      targetAudience: targetAudienceParts.length > 0 ? targetAudienceParts.join(' | ') : undefined,
      colors: colors.length > 0 ? colors : undefined,
      websiteUrl: data.website || undefined,
      supportedLanguages: data.languages?.length ? data.languages : undefined,
      keywords: keywords.length > 0 ? keywords : undefined,
      brandVoice: this.mapBrandVoice(data.brandTone),
    };
  }

  private mapBrandVoice(tones: string[] | undefined): BrandVoice | undefined {
    const first = tones?.[0];
    if (!first) return undefined;

    const map: Record<string, BrandVoice> = {
      'احترافي': 'Professional',
      'ودود': 'Friendly',
      'جريء': 'Bold',
      'مرح': 'Playful',
      'أنيق': 'Formal',
      'ملهم': 'Professional',
      'تثقيفي': 'Professional',
      'مبتكر': 'Bold',
      'محفز': 'Playful',
      'ذكي ومضحك': 'Playful',
    };

    return map[first];
  }

  private buildObjective(data: OnboardingData): string {
    const parts: string[] = [];
    if (data.campaignGoal) parts.push(`الهدف: ${data.campaignGoal}`);
    if (data.mainMessage) parts.push(`الرسالة الرئيسية: ${data.mainMessage}`);
    if (data.campaignDescription) parts.push(data.campaignDescription);
    if (data.targetDescription) parts.push(`الجمهور المستهدف: ${data.targetDescription}`);
    if (data.uniqueValue) parts.push(`القيمة المميزة: ${data.uniqueValue}`);
    if (data.successMetrics?.length) parts.push(`مؤشرات النجاح: ${data.successMetrics.join('، ')}`);
    return parts.join(' | ') || 'حملة تسويقية جديدة';
  }

  private mapPlatforms(values: string[] | undefined): string[] {
    if (!values || values.length === 0) return ['Instagram'];
    return values.map((v) => v.charAt(0).toUpperCase() + v.slice(1).toLowerCase());
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
