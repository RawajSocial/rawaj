import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { GsapRevealDirective } from '../../../shared/directives/gsap-reveal.directive';

@Component({
  selector: 'app-hero',
  imports: [GsapRevealDirective, RouterLink],
  templateUrl: './hero.html',
  styleUrl: './hero.css',
})
export class Hero {}
