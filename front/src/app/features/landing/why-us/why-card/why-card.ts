import { Component, Input } from '@angular/core';
import { RevealDirective } from '../../../../shared/directives/reveal.directive';

@Component({
  selector: 'app-why-card',
  imports: [RevealDirective],
  templateUrl: './why-card.html',
  styleUrl: './why-card.css',
})
export class WhyCard {
  @Input() iconClass: string = '';
  @Input() title: string = '';
  @Input() description: string = '';
}
