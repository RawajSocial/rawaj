import { CommonModule } from '@angular/common';
import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-onboarding-step-actions',
  imports: [CommonModule],
  templateUrl: './onboarding-step-actions.html',
  styleUrl: './onboarding-step-actions.css',
})
export class OnboardingStepActions {
  readonly showBack = input(true);
  readonly showNext = input(true);
  readonly backLabel = input('رجوع');
  readonly nextLabel = input('التالي');
  readonly nextDisabled = input(false);

  readonly back = output<void>();
  readonly next = output<void>();
}
