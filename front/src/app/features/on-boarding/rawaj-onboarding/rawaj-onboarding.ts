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

  constructor() {
    this.currentStep = signal(this.loadSavedStep());
    this.onboardingData.set(this.loadOnboardingData());
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
    const data = this.onboardingData();
    const plans = this.loadPlans();
    const name  = `خطة ${plans.length + 1}${data.brandName ? ' — ' + data.brandName : ''}`;
    plans.push({ id: `plan-${Date.now()}`, createdAt: Date.now(), name, data });
    localStorage.setItem('rawaj.plans', JSON.stringify(plans));
    localStorage.removeItem(this.stepStorageKey);
    localStorage.setItem('rawaj.generating', 'true');
    void this.router.navigate(['/dashboard/marketing-plan']);
  }

  private loadPlans(): SavedPlan[] {
    try { return JSON.parse(localStorage.getItem('rawaj.plans') ?? '[]') as SavedPlan[]; }
    catch { return []; }
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

type SavedPlan = { id: string; createdAt: number; name: string; data: OnboardingData };

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
