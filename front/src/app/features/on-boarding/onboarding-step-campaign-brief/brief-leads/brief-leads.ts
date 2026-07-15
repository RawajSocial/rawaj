import { Component, input, output } from '@angular/core';
import { RevealDirective } from '../../../../shared/directives/reveal.directive';

type OnboardingData = {
  leadAction?: string;
  hasLandingPage?: string;
  landingPageUrl?: string;
  brandStatusForLeads?: string;
  contentFeeling?: string;
};

@Component({
  selector: 'app-brief-leads',
  imports: [RevealDirective],
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
