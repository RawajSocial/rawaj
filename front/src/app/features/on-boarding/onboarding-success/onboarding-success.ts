import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { GsapRevealDirective } from '../../../shared/directives/gsap-reveal.directive';

@Component({
  selector: 'app-onboarding-success',
  templateUrl: './onboarding-success.html',
  imports:[GsapRevealDirective],
  styleUrl: './onboarding-success.css',
})
export class OnboardingSuccess {
  private readonly router = inject(Router);

  protected goToDashboard(): void {
    void this.router.navigate(['/dashboard']);
  }
}
