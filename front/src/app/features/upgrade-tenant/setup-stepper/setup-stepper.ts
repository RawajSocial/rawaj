import { Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-setup-stepper',
  imports: [],
  templateUrl: './setup-stepper.html',
  styleUrl: './setup-stepper.css',
})
export class SetupStepper {
  readonly currentStep = input(1);
  readonly totalSteps = input(4);
  readonly steps = input<string[]>([]);

  protected readonly indices = computed(() =>
    Array.from({ length: this.totalSteps() }, (_, i) => i + 1)
  );

  protected isCompleted(n: number): boolean { return n < this.currentStep(); }
  protected isActive(n: number): boolean { return n === this.currentStep(); }
  protected isLineCompleted(n: number): boolean { return n < this.currentStep(); }
}
