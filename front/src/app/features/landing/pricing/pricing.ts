import { Component } from '@angular/core';
import { RevealDirective } from '../../../shared/directives/reveal.directive';

@Component({
  selector: 'app-pricing',
  imports: [RevealDirective],
  templateUrl: './pricing.html',
  styleUrl: './pricing.css',
})
export class Pricing {
  isAnnual = false;

  toggleBilling(value: boolean) {
    this.isAnnual = value;
  }
}
