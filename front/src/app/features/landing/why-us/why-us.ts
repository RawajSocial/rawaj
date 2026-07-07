import { Component, signal } from '@angular/core';
import { WhyUsItem } from '../../../model/why-us-item.model';
import { WhyCard } from './why-card/why-card';
import { RevealDirective } from '../../../shared/directives/reveal.directive';

@Component({
  selector: 'app-why-us',
  imports: [WhyCard,RevealDirective],
  templateUrl: './why-us.html',
  styleUrl: './why-us.css',
})
export class WhyUs {
  readonly whyItems = signal<WhyUsItem[]>([
    {
      id: 1,
      iconClass: 'fa-solid fa-comments',
      title: 'أنت تتكلم، نحن نستمع',
      description: 'نفهم هدف نشاطك أولًا، ثم نبني القرار المناسب لحملتك.',
    },
    {
      id: 2,
      iconClass: 'fa-solid fa-gears',
      title: 'نحن نُقدّر النزاهة',
      description: 'عمل واضح وخطة مدروسة بدون وعود تسويقية مبالغ فيها.',
    },
    {
      id: 3,
      iconClass: 'fa-solid fa-magnifying-glass-chart',
      title: 'نحن خبراء SEO',
      description: 'نرفع ظهورك في نتائج البحث بخطوات عملية قابلة للقياس.',
    },
    {
      id: 4,
      iconClass: 'fa-solid fa-calendar-check',
      title: 'التزام بالمواعيد',
      description: 'نسلّم في الوقت ونتابع الأداء باستمرار مع فريقك.',
    },
    {
      id: 5,
      iconClass: 'fa-solid fa-pen-ruler',
      title: 'تصاميم احترافية',
      description: 'هوية بصرية واضحة تعكس علامتك وتدعم رسالتك التسويقية.',
    },
    {
      id: 6,
      iconClass: 'fa-solid fa-globe',
      title: 'خبراء WordPress',
      description: 'نبني مواقع مرنة وسريعة وسهلة الإدارة لفريق المحتوى.',
    },
  ]);
}
