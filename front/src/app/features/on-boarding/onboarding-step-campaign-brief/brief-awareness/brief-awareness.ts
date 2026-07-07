import { Component, input, output } from '@angular/core';
import { RevealDirective } from '../../../../shared/directives/reveal.directive';

type OnboardingData = {
  mainMessage?: string;
};

@Component({
  selector: 'app-brief-awareness',
  imports: [RevealDirective],
  templateUrl: './brief-awareness.html',
  styleUrls: ['../../onboarding-shared.css', '../brief-shared.css'],
})
export class BriefAwareness {
  readonly data = input<OnboardingData | null>(null);
  readonly dataChange = output<Partial<OnboardingData>>();

  protected emit(partial: Partial<OnboardingData>): void {
    this.dataChange.emit(partial);
  }
}
