import { Component, input, output } from '@angular/core';
import { RevealDirective } from '../../../../shared/directives/reveal.directive';

type OnboardingData = {
  campaignName?: string;
  campaignGoal?: string;
  campaignStartDate?: string;
  campaignDuration?: string;
};

@Component({
  selector: 'app-brief-common',
  imports: [RevealDirective],
  templateUrl: './brief-common.html',
  styleUrls: ['../../onboarding-shared.css', '../brief-shared.css'],
})
export class BriefCommon {
  readonly data = input<OnboardingData | null>(null);
  readonly dataChange = output<Partial<OnboardingData>>();

  protected readonly durations = [
    'أسبوع واحد', 'أسبوعان', 'شهر واحد', 'شهران', '3 أشهر', 'مستمرة',
  ];

  protected emit(partial: Partial<OnboardingData>): void {
    this.dataChange.emit(partial);
  }
}
