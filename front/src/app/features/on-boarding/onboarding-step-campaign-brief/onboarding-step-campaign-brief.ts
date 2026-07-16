import { Component, computed, input, output } from '@angular/core';
import { OnboardingStepHeader } from '../onboarding-step-header/onboarding-step-header';
import { OnboardingStepActions } from '../onboarding-step-actions/onboarding-step-actions';
import { StepBadge } from '../../../shared/components/step-badge/step-badge';
import { StepHeading } from '../../../shared/components/step-heading/step-heading';
import { GsapRevealDirective } from '../../../shared/directives/gsap-reveal.directive';
import { BriefNewBusiness } from './brief-new-business/brief-new-business';
import { BriefNewProduct } from './brief-new-product/brief-new-product';
import { BriefDriveSales } from './brief-drive-sales/brief-drive-sales';
import { BriefSeasonal } from './brief-seasonal/brief-seasonal';
import { BriefLeads } from './brief-leads/brief-leads';
import { BriefAwareness } from './brief-awareness/brief-awareness';
import { BriefOther } from './brief-other/brief-other';
import { BriefCommon } from './brief-common/brief-common';

type CampaignMeta = { id: string; title: string; icon: string };

@Component({
  selector: 'app-onboarding-step-campaign-brief',
  imports: [
    GsapRevealDirective,
    OnboardingStepHeader,
    OnboardingStepActions,
    StepBadge,
    StepHeading,
    BriefNewBusiness,
    BriefNewProduct,
    BriefDriveSales,
    BriefSeasonal,
    BriefLeads,
    BriefAwareness,
    BriefOther,
    BriefCommon,
  ],
  templateUrl: './onboarding-step-campaign-brief.html',
  styleUrl: './onboarding-step-campaign-brief.css',
})
export class OnboardingStepCampaignBrief {
  readonly currentStep = input(2);
  readonly totalSteps = input(7);
  readonly data = input<OnboardingData | null>(null);
  readonly next = output<void>();
  readonly back = output<void>();
  readonly dataChange = output<Partial<OnboardingData>>();

  private readonly campaignMeta: CampaignMeta[] = [
    { id: 'new-business', title: 'إطلاق نشاط جديد', icon: 'fa-solid fa-rocket' },
    { id: 'new-product',  title: 'إطلاق منتج جديد',  icon: 'fa-solid fa-box-open' },
    { id: 'drive-sales',  title: 'زيادة المبيعات',    icon: 'fa-solid fa-chart-line' },
    { id: 'seasonal',     title: 'حملة موسمية',       icon: 'fa-solid fa-calendar-days' },
    { id: 'leads',        title: 'توليد عملاء',        icon: 'fa-solid fa-users' },
    { id: 'awareness',    title: 'الوعي بالعلامة',     icon: 'fa-solid fa-bullhorn' },
    { id: 'other',        title: 'أخرى',               icon: 'fa-solid fa-ellipsis' },
  ];

  protected readonly selectedType = computed(() => this.data()?.campaignType ?? '');

  protected readonly activeMeta = computed(() =>
    this.campaignMeta.find(m => m.id === this.selectedType())
  );

  protected readonly stepTitle = computed(() =>
    this.activeMeta()?.title ?? 'موجز الحملة'
  );

  protected readonly stepIcon = computed(() =>
    this.activeMeta()?.icon ?? 'fa-solid fa-flag'
  );

  protected readonly canProceed = computed(() => !!this.data()?.campaignStartDate);

  protected emit(partial: Partial<OnboardingData>): void {
    this.dataChange.emit(partial);
  }

  protected onNext(): void {
    this.next.emit();
  }

  protected onBack(): void {
    this.back.emit();
  }
}

type OnboardingData = {
  campaignType?: string;
  campaignName?: string;
  campaignGoal?: string;
  campaignStartDate?: string;
  campaignDuration?: string;
  businessEstablishDate?: string;
  brandIdentityReady?: string;
  businessLaunchDate?: string;
  productName?: string;
  productCategory?: string;
  productAvailability?: string;
  productPricePoint?: string;
  salesScope?: string;
  hasOffer?: string;
  offerDetails?: string;
  salesPeriod?: string;
  occasion?: string;
  seasonStart?: string;
  seasonEnd?: string;
  leadAction?: string;
  hasLandingPage?: string;
  landingPageUrl?: string;
  brandStatusForLeads?: string;
  contentFeeling?: string;
  mainMessage?: string;
  campaignDescription?: string;
};
