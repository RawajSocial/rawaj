import { Component, input, output } from '@angular/core';
import { GsapRevealDirective } from '../../../../shared/directives/gsap-reveal.directive';

type OnboardingData = {
  leadAction?: string;
  hasLandingPage?: string;
  landingPageUrl?: string;
  contentFeeling?: string;
};

@Component({
  selector: 'app-brief-leads',
  imports: [GsapRevealDirective],
  templateUrl: './brief-leads.html',
  styleUrls: ['../../onboarding-shared.css', '../brief-shared.css'],
})
export class BriefLeads {
  readonly data = input<OnboardingData | null>(null);
  readonly dataChange = output<Partial<OnboardingData>>();

  protected readonly leadActions = [
    'تعبئة نموذج', 'اتصال هاتفي', 'واتساب', 'حجز موعد', 'زيارة المتجر',
  ];

  protected emit(partial: Partial<OnboardingData>): void {
    this.dataChange.emit(partial);
  }
}
