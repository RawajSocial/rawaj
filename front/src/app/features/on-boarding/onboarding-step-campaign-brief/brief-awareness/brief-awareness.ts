import { Component, input, output } from '@angular/core';
import { GsapRevealDirective } from '../../../../shared/directives/gsap-reveal.directive';

type OnboardingData = {
  mainMessage?: string;
};

@Component({
  selector: 'app-brief-awareness',
  imports: [GsapRevealDirective],
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
