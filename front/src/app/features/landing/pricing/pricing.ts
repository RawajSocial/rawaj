import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { GsapRevealDirective } from '../../../shared/directives/gsap-reveal.directive';

@Component({
  selector: 'app-pricing',
  imports: [GsapRevealDirective],
  templateUrl: './pricing.html',
  styleUrl: './pricing.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Pricing {
  readonly isAnnual = signal(false);

  toggleBilling(value: boolean): void {
    this.isAnnual.set(value);
  }
}
