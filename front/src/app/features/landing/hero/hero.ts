import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { RevealDirective } from '../../../shared/directives/reveal.directive';
import { ɵInternalFormsSharedModule } from "@angular/forms";

@Component({
  selector: 'app-hero',
  imports: [RevealDirective, ɵInternalFormsSharedModule],
  templateUrl: './hero.html',
  styleUrl: './hero.css',
})
export class Hero {
  private readonly router = inject(Router);

  protected goToDashboard(): void {
    void this.router.navigate(['/dashboard']);
  }

  protected scrollToSolutions(): void {
    document.getElementById('solutions')?.scrollIntoView({ behavior: 'smooth' });
  }
}
