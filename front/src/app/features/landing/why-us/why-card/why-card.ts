import { Component, Input } from '@angular/core';
import { GsapRevealDirective } from '../../../../shared/directives/gsap-reveal.directive';

@Component({
  selector: 'app-why-card',
  imports: [GsapRevealDirective],
  templateUrl: './why-card.html',
  styleUrl: './why-card.css',
})
export class WhyCard {
  @Input() iconClass: string = '';
  @Input() title: string = '';
  @Input() description: string = '';
}
