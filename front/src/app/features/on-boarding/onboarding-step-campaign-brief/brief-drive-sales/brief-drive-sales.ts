import { Component, input, output } from '@angular/core';
import { RevealDirective } from '../../../../shared/directives/reveal.directive';

type OnboardingData = {
  salesScope?: string;
  hasOffer?: string;
  offerDetails?: string;
  salesPeriod?: string;
};

@Component({
  selector: 'app-brief-drive-sales',
  imports: [RevealDirective],
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
