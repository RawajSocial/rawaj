import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { RevealDirective } from '../../../shared/directives/reveal.directive';

@Component({
  selector: 'app-pricing',
  imports: [RevealDirective],
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
