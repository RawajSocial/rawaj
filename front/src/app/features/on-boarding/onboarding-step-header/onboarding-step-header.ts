import { CommonModule } from '@angular/common';
import { Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-onboarding-step-header',
  imports: [CommonModule],
  templateUrl: './onboarding-step-header.html',
  styleUrl: './onboarding-step-header.css',
})
export class OnboardingStepHeader {
  readonly currentStep = input(1);
  readonly totalSteps = input(6);
  readonly title = input('');

  protected readonly progressPercent = computed(() => (this.currentStep() / this.totalSteps()) * 100);
}
