import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { GsapRevealDirective } from '../../../shared/directives/gsap-reveal.directive';

interface ProblemItem {
  id: number;
  title: string;
  points: string[];
}

@Component({
  selector: 'app-problems',
  imports: [GsapRevealDirective],
  templateUrl: './problems.html',
  styleUrl: './problems.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Problems {
  readonly problems = signal<ProblemItem[]>([
    {
      id: 1,
      title: 'عمل يدوي في كل مكان',
      points: [
        'الفريق يكرر نفس المهام يدويًا كل يوم',
        'جهد تشغيلي غير ضروري يستهلك الوقت',
        'فرص الأتمتة تبقى غير مستغلة',
      ],
    },
    {
      id: 2,
      title: 'أدوات غير مترابطة',
      points: [
        'الأدوات لا تتكامل مع بعضها بشكل صحيح',
        'البيانات مبعثرة بين أنظمة منفصلة',
        'التنقل المستمر بين الأدوات يشتت التركيز',
      ],
    },
    {
      id: 3,
      title: 'عمليات غير فعالة',
      points: [
        'المعلومات مبعثرة عبر منصات متعددة',
        'التقارير اليدوية تُبطئ اتخاذ القرار',
        'غياب الرؤية اللحظية لأداء الحملات',
      ],
    },
  ]);
}
