import { Component } from '@angular/core';
import { RevealDirective } from '../../../shared/directives/reveal.directive';

@Component({
  selector: 'app-cta',
  imports: [RevealDirective],
  templateUrl: './cta.html',
  styleUrl: './cta.css',
})
export class Cta {}
