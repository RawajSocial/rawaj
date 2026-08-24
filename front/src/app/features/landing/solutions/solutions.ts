import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { GsapRevealDirective } from '../../../shared/directives/gsap-reveal.directive';

interface SolutionItem {
  id: number;
  iconClass: string;
  title: string;
  description: string;
}

@Component({
  selector: 'app-solutions',
  imports: [GsapRevealDirective],
  templateUrl: './solutions.html',
  styleUrl: './solutions.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Solutions {
  readonly solutions = signal<SolutionItem[]>([
    {
      id: 1,
      iconClass: 'fa-solid fa-arrow-trend-up',
      title: 'نمو متكيف',
      description: 'تتوسع عملياتك مع الطلب مع الحفاظ على كفاءة عالية.',
    },
    {
      id: 2,
      iconClass: 'fa-solid fa-bolt',
      title: 'تنفيذ سريع',
      description: 'تتقدم الأعمال تلقائيًا مع تقليل التأخير ومتابعة دقيقة.',
    },
    {
      id: 3,
      iconClass: 'fa-solid fa-gauge-high',
      title: 'عمل أسرع',
      description: 'سير عمل آلي يساعد فريقك على التحرك بذكاء وسرعة.',
    },
    {
      id: 4,
      iconClass: 'fa-solid fa-circle-check',
      title: 'نتائج ثابتة',
      description: 'عمليات تعمل بسلاسة لضمان نتائج موثوقة ومتسقة.',
    },
  ]);
}
