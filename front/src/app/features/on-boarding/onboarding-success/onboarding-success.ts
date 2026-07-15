import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { RevealDirective } from '../../../shared/directives/reveal.directive';

@Component({
  selector: 'app-onboarding-success',
  templateUrl: './onboarding-success.html',
  imports:[RevealDirective],
  styleUrl: './onboarding-success.css',
})
export class OnboardingSuccess {
  private readonly router = inject(Router);

  protected goToDashboard(): void {
    void this.router.navigate(['/dashboard']);
  }
}
