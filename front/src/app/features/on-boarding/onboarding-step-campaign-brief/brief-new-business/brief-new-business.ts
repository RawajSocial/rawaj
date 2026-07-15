import { Component, input, output } from '@angular/core';
import { RevealDirective } from '../../../../shared/directives/reveal.directive';

type OnboardingData = {
  businessEstablishDate?: string;
  businessLaunchDate?: string;
  brandIdentityReady?: string;
};

@Component({
  selector: 'app-brief-new-business',
  imports: [RevealDirective],
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
