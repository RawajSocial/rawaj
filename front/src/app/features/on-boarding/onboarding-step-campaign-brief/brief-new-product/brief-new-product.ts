import { Component, input, output } from '@angular/core';
import { GsapRevealDirective } from '../../../../shared/directives/gsap-reveal.directive';

type OnboardingData = {
  productName?: string;
  productCategory?: string;
  productPricePoint?: string;
  productAvailability?: string;
};

@Component({
  selector: 'app-brief-new-product',
  imports: [GsapRevealDirective],
  templateUrl: './brief-new-product.html',
  styleUrls: ['../../onboarding-shared.css', '../brief-shared.css'],
})
export class BriefNewProduct {
  readonly data = input<OnboardingData | null>(null);
  readonly dataChange = output<Partial<OnboardingData>>();

  protected emit(partial: Partial<OnboardingData>): void {
    this.dataChange.emit(partial);
  }
}
