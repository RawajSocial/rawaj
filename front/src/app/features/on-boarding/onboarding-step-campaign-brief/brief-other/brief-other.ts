import { Component, input, output } from '@angular/core';
import { RevealDirective } from '../../../../shared/directives/reveal.directive';

type OnboardingData = {
  campaignDescription?: string;
};

@Component({
  selector: 'app-brief-other',
  imports: [RevealDirective],
  templateUrl: './brief-other.html',
  styleUrls: ['../../onboarding-shared.css', '../brief-shared.css'],
})
export class BriefOther {
  readonly data = input<OnboardingData | null>(null);
  readonly dataChange = output<Partial<OnboardingData>>();

  protected emit(partial: Partial<OnboardingData>): void {
    this.dataChange.emit(partial);
  }
}
