import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { RevealDirective } from '../../../shared/directives/reveal.directive';

@Component({
  selector: 'app-pricing',
  imports: [RevealDirective, RouterLink],
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
