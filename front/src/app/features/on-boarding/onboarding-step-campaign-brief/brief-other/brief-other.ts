import { Component, input, output } from '@angular/core';
import { GsapRevealDirective } from '../../../../shared/directives/gsap-reveal.directive';

type OnboardingData = {
  campaignDescription?: string;
};

@Component({
  selector: 'app-brief-other',
  imports: [GsapRevealDirective],
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
