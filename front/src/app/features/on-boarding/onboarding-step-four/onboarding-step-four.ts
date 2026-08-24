import { Component, computed, input, output } from '@angular/core';
import { OnboardingStepHeader } from '../onboarding-step-header/onboarding-step-header';
import { OnboardingStepActions } from '../onboarding-step-actions/onboarding-step-actions';
import { StepBadge } from '../../../shared/components/step-badge/step-badge';
import { StepHeading } from '../../../shared/components/step-heading/step-heading';
import { GsapRevealDirective } from '../../../shared/directives/gsap-reveal.directive';

@Component({
  selector: 'app-onboarding-step-four',
  imports: [GsapRevealDirective, OnboardingStepHeader, OnboardingStepActions, StepBadge, StepHeading],
  templateUrl: './onboarding-step-four.html',
  styleUrl: './onboarding-step-four.css',
})
export class OnboardingStepFour {
  readonly currentStep = input(4);
  readonly totalSteps = input(7);
  readonly data = input<OnboardingData | null>(null);
  readonly next = output<void>();
  readonly back = output<void>();
  readonly dataChange = output<Partial<OnboardingData>>();

  protected readonly genderOptions = [
    { value: 'female', label: 'نساء',    icon: 'fa-solid fa-person-dress',  className: 'gender-female' },
    { value: 'male',   label: 'رجال',    icon: 'fa-solid fa-person',        className: 'gender-male' },
    { value: 'all',    label: 'الجنسين', icon: 'fa-solid fa-people-group',  className: 'gender-all' },
  ];

  protected readonly customerTypeOptions = [
    { value: 'b2c',  label: 'أفراد (B2C)',  icon: 'fa-solid fa-user',      className: 'ctype-b2c' },
    { value: 'b2b',  label: 'شركات (B2B)',  icon: 'fa-solid fa-building',  className: 'ctype-b2b' },
    { value: 'both', label: 'أفراد وشركات', icon: 'fa-solid fa-handshake', className: 'ctype-both' },
  ];

  protected readonly ageRangeOptions = [
    '13 – 17', '18 – 24', '25 – 34', '35 – 44', '45 – 54', '55 – 64', '65+',
  ];

  protected readonly incomeLevelOptions = [
    { value: 'budget',   label: 'اقتصادي',    icon: 'fa-solid fa-coins',     className: 'income-budget' },
    { value: 'mid',      label: 'متوسط',       icon: 'fa-solid fa-wallet',    className: 'income-mid' },
    { value: 'high',     label: 'مرتفع',       icon: 'fa-solid fa-gem',       className: 'income-high' },
    { value: 'luxury',   label: 'فاخر جداً',   icon: 'fa-solid fa-crown',     className: 'income-luxury' },
    { value: 'business', label: 'أصحاب أعمال', icon: 'fa-solid fa-briefcase', className: 'income-business' },
  ];

  protected readonly locationOptions = [
    { value: 'local',         label: 'محلي (مدينة واحدة)', icon: 'fa-solid fa-location-dot',   className: 'loc-local' },
    { value: 'national',      label: 'وطني (داخل الدولة)', icon: 'fa-solid fa-flag',            className: 'loc-national' },
    { value: 'international', label: 'دولي',                icon: 'fa-solid fa-earth-americas', className: 'loc-international' },
  ];

  protected readonly educationOptions = [
    'ثانوي أو أقل', 'طالب جامعي', 'خريج جامعي', 'دراسات عليا',
  ];

  protected readonly interestOptions = [
    'الموضة والأزياء', 'الصحة واللياقة', 'التقنية والإلكترونيات', 'السفر والسياحة',
    'الطعام والمطبخ', 'الجمال والعناية', 'الأسرة والأطفال', 'الرياضة',
    'التعليم والتطوير', 'الفن والتصميم', 'الأعمال والريادة', 'الترفيه والألعاب',
  ];

  protected readonly buyingBehaviorOptions = [
    { value: 'impulse',  label: 'مشتري اندفاعي',     icon: 'fa-solid fa-bolt',             className: 'beh-impulse' },
    { value: 'research', label: 'يبحث كثيراً',        icon: 'fa-solid fa-magnifying-glass', className: 'beh-research' },
    { value: 'loyal',    label: 'وفي للعلامة',         icon: 'fa-solid fa-heart',            className: 'beh-loyal' },
    { value: 'deals',    label: 'يبحث عن العروض',     icon: 'fa-solid fa-percent',          className: 'beh-deals' },
    { value: 'social',   label: 'متأثر برأي الآخرين', icon: 'fa-solid fa-users',            className: 'beh-social' },
    { value: 'quality',  label: 'يهتم بالجودة أولاً', icon: 'fa-solid fa-star',             className: 'beh-quality' },
  ];

  protected readonly platformOptions = [
    { value: 'instagram', label: 'Instagram',   icon: 'fa-brands fa-instagram',  className: 'aud-instagram' },
    { value: 'facebook',  label: 'Facebook',    icon: 'fa-brands fa-facebook',   className: 'aud-facebook' },
  ];

  protected readonly selectedAgeRanges  = computed(() => this.data()?.ageRanges    ?? []);
  protected readonly selectedIncome     = computed(() => this.data()?.incomeLevel   ?? []);
  protected readonly selectedInterests  = computed(() => this.data()?.interests     ?? []);
  protected readonly selectedBehaviors  = computed(() => this.data()?.buyingBehavior ?? []);
  protected readonly selectedPlatforms  = computed(() => this.data()?.audiencePlatforms ?? []);

  protected toggle(key: MultiKey, value: string): void {
    const current = [...(this.data()?.[key] ?? [])];
    const idx = current.indexOf(value);
    if (idx >= 0) current.splice(idx, 1);
    else current.push(value);
    this.emit({ [key]: current } as Partial<OnboardingData>);
  }

  protected emit(partial: Partial<OnboardingData>): void {
    this.dataChange.emit(partial);
  }

  protected onNext(): void { this.next.emit(); }
  protected onBack(): void { this.back.emit(); }
}

type MultiKey = 'ageRanges' | 'incomeLevel' | 'interests' | 'buyingBehavior' | 'audiencePlatforms';

type OnboardingData = {
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
};
