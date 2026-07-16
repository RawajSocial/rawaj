import { Component, input, output } from '@angular/core';
import { GsapRevealDirective } from '../../../../shared/directives/gsap-reveal.directive';

type OnboardingData = {
  salesScope?: string;
  hasOffer?: string;
  offerDetails?: string;
  salesPeriod?: string;
};

@Component({
  selector: 'app-brief-drive-sales',
  imports: [GsapRevealDirective],
  templateUrl: './brief-drive-sales.html',
  styleUrls: ['../../onboarding-shared.css', '../brief-shared.css'],
})
export class BriefDriveSales {
  readonly data = input<OnboardingData | null>(null);
  readonly dataChange = output<Partial<OnboardingData>>();

  protected emit(partial: Partial<OnboardingData>): void {
    this.dataChange.emit(partial);
  }
}
