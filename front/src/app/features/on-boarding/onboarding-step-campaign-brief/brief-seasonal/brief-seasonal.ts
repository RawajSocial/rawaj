import { Component, input, output } from '@angular/core';
import { GsapRevealDirective } from '../../../../shared/directives/gsap-reveal.directive';

type OnboardingData = {
  occasion?: string;
  seasonStart?: string;
  seasonEnd?: string;
};

@Component({
  selector: 'app-brief-seasonal',
  imports: [GsapRevealDirective],
  templateUrl: './brief-seasonal.html',
  styleUrls: ['../../onboarding-shared.css', '../brief-shared.css'],
})
export class BriefSeasonal {
  readonly data = input<OnboardingData | null>(null);
  readonly dataChange = output<Partial<OnboardingData>>();

  protected readonly occasions = [
    'رمضان', 'عيد الفطر', 'عيد الأضحى', 'يوم الأم', 'العودة للمدرسة',
    'اليوم الوطني', 'الجمعة البيضاء', 'عيد الحب', 'مناسبة مخصصة',
  ];

  protected emit(partial: Partial<OnboardingData>): void {
    this.dataChange.emit(partial);
  }
}
