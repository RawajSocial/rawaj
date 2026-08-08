import { Component, computed, HostListener, input, output } from '@angular/core';
import { OnboardingStepHeader } from '../onboarding-step-header/onboarding-step-header';
import { OnboardingStepActions } from '../onboarding-step-actions/onboarding-step-actions';
import { StepBadge } from '../../../shared/components/step-badge/step-badge';
import { StepHeading } from '../../../shared/components/step-heading/step-heading';
import { GsapRevealDirective } from '../../../shared/directives/gsap-reveal.directive';

@Component({
  selector: 'app-onboarding-step-five',
  imports: [GsapRevealDirective, OnboardingStepHeader, OnboardingStepActions, StepBadge, StepHeading],
  templateUrl: './onboarding-step-five.html',
  styleUrl: './onboarding-step-five.css',
})
export class OnboardingStepFive {
  readonly currentStep = input(5);
  readonly totalSteps = input(7);
  readonly data = input<OnboardingData | null>(null);
  readonly next = output<void>();
  readonly back = output<void>();
  readonly dataChange = output<Partial<OnboardingData>>();

  protected readonly positioningOptions: ChipOption[] = [
    { value: 'affordable',  label: 'أرخص من المنافسين',    icon: 'fa-solid fa-coins',         className: 'pos-affordable' },
    { value: 'premium',     label: 'أكثر راقياً وجودةً',    icon: 'fa-solid fa-gem',           className: 'pos-premium' },
    { value: 'consistent',  label: 'أكثر ثباتاً واتساقاً',  icon: 'fa-solid fa-shield-halved', className: 'pos-consistent' },
    { value: 'reliable',    label: 'أكثر موثوقيةً وثقةً',   icon: 'fa-solid fa-star',          className: 'pos-reliable' },
    { value: 'personal',    label: 'أكثر شخصيةً ومحليةً',   icon: 'fa-solid fa-heart',         className: 'pos-personal' },
    { value: 'sustainable', label: 'أكثر استدامةً',         icon: 'fa-solid fa-leaf',          className: 'pos-sustainable' },
    { value: 'convenient',  label: 'أكثر سهولةً وراحةً',    icon: 'fa-solid fa-bolt',          className: 'pos-convenient' },
  ];

  protected readonly successOptions: ChipOption[] = [
    { value: 'followers',  label: 'متابعون أكثر',    icon: 'fa-solid fa-users',       className: 'succ-followers' },
    { value: 'visits',     label: 'زيارات الموقع',    icon: 'fa-solid fa-globe',       className: 'succ-visits' },
    { value: 'sales',      label: 'مبيعات أكثر',      icon: 'fa-solid fa-sack-dollar', className: 'succ-sales' },
    { value: 'leads',      label: 'عملاء محتملون',    icon: 'fa-solid fa-bullseye',    className: 'succ-leads' },
    { value: 'downloads',  label: 'تحميلات أكثر',     icon: 'fa-solid fa-download',    className: 'succ-downloads' },
    { value: 'enquiries',  label: 'استفسارات أكثر',   icon: 'fa-solid fa-envelope',    className: 'succ-enquiries' },
    { value: 'awareness',  label: 'وعي بالعلامة',     icon: 'fa-solid fa-bullhorn',    className: 'succ-awareness' },
  ];

  protected readonly budgetOptions = [
    'أقل من 500 ريال',
    '500 – 2,000 ريال',
    '2,000 – 5,000 ريال',
    '5,000 – 15,000 ريال',
    '15,000 – 30,000 ريال',
    'أكثر من 30,000 ريال',
    'لا ميزانية محددة بعد',
  ];

  protected readonly rankablePlatforms = [
    { value: 'instagram', label: 'Instagram',   icon: 'fa-brands fa-instagram' },
    { value: 'facebook',  label: 'Facebook',    icon: 'fa-brands fa-facebook' },
  ];

  protected budgetMenuOpen = false;

  protected readonly selectedSuccessMetrics = computed(() => this.data()?.successMetrics ?? []);
  protected readonly platformRanking        = computed(() => this.data()?.platformRanking ?? []);

  protected toggleSuccess(value: string): void {
    const current = [...this.selectedSuccessMetrics()];
    const idx = current.indexOf(value);
    if (idx >= 0) current.splice(idx, 1);
    else current.push(value);
    this.emit({ successMetrics: current });
  }

  protected rankPlatform(value: string): void {
    const current = [...this.platformRanking()];
    const idx = current.indexOf(value);
    if (idx >= 0) current.splice(idx, 1);
    else current.push(value);
    this.emit({ platformRanking: current });
  }

  protected getRank(value: string): number {
    return this.platformRanking().indexOf(value) + 1;
  }

  protected platformLabel(value: string): string {
    return this.rankablePlatforms.find(p => p.value === value)?.label ?? value;
  }

  protected toggleBudgetMenu(): void { this.budgetMenuOpen = !this.budgetMenuOpen; }

  protected selectBudget(option: string): void {
    this.emit({ monthlyBudget: option });
    this.budgetMenuOpen = false;
  }

  protected emit(partial: Partial<OnboardingData>): void { this.dataChange.emit(partial); }
  protected onNext(): void { this.next.emit(); }
  protected onBack(): void { this.back.emit(); }

  @HostListener('document:click')
  protected closeMenus(): void { this.budgetMenuOpen = false; }

  @HostListener('document:keydown.escape')
  protected closeMenusOnEscape(): void { this.budgetMenuOpen = false; }
}

type ChipOption = { value: string; label: string; icon: string; className: string };

type OnboardingData = {
  positioningVs?: string;
  campaignOutcome?: string;
  successMetrics?: string[];
  brandAdmire1?: string;
  brandAdmire2?: string;
  brandAdmire3?: string;
  monthlyBudget?: string;
  tagline?: string;
  platformRanking?: string[];
};
