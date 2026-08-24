import { Component, signal } from '@angular/core';
import { WhyUsItem } from '../../../model/why-us-item.model';
import { WhyCard } from './why-card/why-card';
import { GsapRevealDirective } from '../../../shared/directives/gsap-reveal.directive';

@Component({
  selector: 'app-why-us',
  imports: [WhyCard,GsapRevealDirective],
  templateUrl: './why-us.html',
  styleUrl: './why-us.css',
})
export class WhyUs {
  readonly whyItems = signal<WhyUsItem[]>([
    {
      id: 1,
      iconClass: 'fa-solid fa-wand-magic-sparkles',
      title: 'محتوى جاهز خلال ثوانٍ',
      description: 'الذكاء الاصطناعي يُنشئ لك صورًا وفيديوهات وكابشنز احترافية دون الحاجة لمصمم أو كاتب محتوى.',
    },
    {
      id: 2,
      iconClass: 'fa-solid fa-share-nodes',
      title: 'نشر تلقائي على كل المنصات',
      description: 'انشر على إنستغرام وفيسبوك وتيك توك وسناب شات ولينكدإن من مكان واحد، بضغطة زر.',
    },
    {
      id: 3,
      iconClass: 'fa-solid fa-chart-line',
      title: 'خطة تسويقية مبنية على بياناتك',
      description: 'رواج يحلل نشاطك وجمهورك ليقترح عليك خطة تسويقية جاهزة للتنفيذ من اليوم الأول.',
    },
    {
      id: 4,
      iconClass: 'fa-solid fa-earth-africa',
      title: 'مصمم لسوق الشرق الأوسط',
      description: 'محتوى وتوصيات تراعي لغة وثقافة وسلوك المستهلك العربي، لا نسخ عالمية معرّبة.',
    },
    {
      id: 5,
      iconClass: 'fa-solid fa-users-gear',
      title: 'فريقك كله في مكان واحد',
      description: 'ادعُ مسوّقين لوكالتك، وزّع المهام عليهم، وتابع أداء الحملات من لوحة تحكم واحدة.',
    },
    {
      id: 6,
      iconClass: 'fa-solid fa-coins',
      title: 'تسعير مرن يكبر معك',
      description: 'ابدأ مجانًا، وارفع باقتك فقط عند الحاجة إلى مزيد من الرصيد والمزايا.',
    },
  ]);
}
