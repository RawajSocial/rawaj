import { Component, input, output } from '@angular/core';
import { RevealDirective } from '../../../../shared/directives/reveal.directive';

type OnboardingData = {
  occasion?: string;
  seasonStart?: string;
  seasonEnd?: string;
};

@Component({
  selector: 'app-brief-seasonal',
  imports: [RevealDirective],
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
