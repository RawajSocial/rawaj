import { Component } from '@angular/core';
import { RevealDirective } from '../../../shared/directives/reveal.directive';
import { ɵInternalFormsSharedModule } from "@angular/forms";

@Component({
  selector: 'app-hero',
  imports: [RevealDirective, ɵInternalFormsSharedModule],
  templateUrl: './hero.html',
  styleUrl: './hero.css',
})
export class Hero {}
