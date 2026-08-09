import { Component, input, output } from '@angular/core';
import { GsapRevealDirective } from '../../../../shared/directives/gsap-reveal.directive';

type OnboardingData = {
  businessLaunchDate?: string;
};

@Component({
  selector: 'app-brief-new-business',
  imports: [GsapRevealDirective],
  templateUrl: './brief-new-business.html',
  styleUrls: ['../../onboarding-shared.css', '../brief-shared.css'],
})
export class BriefNewBusiness {
  readonly data = input<OnboardingData | null>(null);
  readonly dataChange = output<Partial<OnboardingData>>();

  protected emit(partial: Partial<OnboardingData>): void {
    this.dataChange.emit(partial);
  }
}
