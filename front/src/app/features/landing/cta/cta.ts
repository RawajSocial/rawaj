import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { GsapRevealDirective } from '../../../shared/directives/gsap-reveal.directive';

@Component({
  selector: 'app-cta',
  imports: [GsapRevealDirective, RouterLink],
  templateUrl: './cta.html',
  styleUrl: './cta.css',
})
export class Cta {}
