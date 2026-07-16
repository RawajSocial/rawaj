import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PageHeader } from '../../../../shared/components/page-header/page-header';
import { SeoService } from '../../../../services/seo.service';

interface Faq {
  q: string;
  a: string;
}

@Component({
  selector: 'app-help-support-page',
  imports: [PageHeader, ReactiveFormsModule],
  templateUrl: './help-support-page.html',
  styleUrls: ['../../dashboard-shared.css', './help-support-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HelpSupportPage {
  private readonly seo = inject(SeoService);

  constructor() {
    this.seo.setPageSeo({
      title: 'المساعدة والدعم | رواج',
      description: 'أسئلة شائعة وتواصل مباشر مع فريق الدعم الفني في رواج.',
      keywords: 'رواج, المساعدة, الدعم الفني, الأسئلة الشائعة',
      path: '/dashboard/help',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });
  }

  protected readonly faqs = signal<Faq[]>([
    { q: 'كيف أبدأ أول حملة تسويقية؟', a: 'ابدأ من خطوة "ابدأ حملة جديدة"، أدخل بيانات نشاطك، وسيقوم رواج بتوليد خطة تسويقية ومحتوى جاهز للنشر.' },
    { q: 'كيف أربط حساباتي على مواقع التواصل؟', a: 'من صفحة الإعدادات يمكنك ربط حسابات إنستغرام وفيسبوك وتيك توك وسناب شات ولينكدإن لنشر المحتوى تلقائيًا.' },
    { q: 'هل يمكنني دعوة مسوّقين للعمل معي؟', a: 'نعم، من صفحة "المستخدمون" يمكنك دعوة أعضاء الفريق وتعيينهم للمشاريع وتحديد صلاحياتهم.' },
    { q: 'كيف يتم احتساب استهلاك الرصيد؟', a: 'يُخصم الرصيد عند توليد المحتوى بالذكاء الاصطناعي. يمكنك متابعة الاستهلاك من صفحة المستخدم أو الفوترة.' },
    { q: 'كيف أغيّر باقتي أو ألغي الاشتراك؟', a: 'من صفحة الفوترة يمكنك الترقية أو التخفيض أو إلغاء الاشتراك في أي وقت.' },
  ]);

  protected readonly openFaq = signal<number | null>(0);

  private readonly fb = new FormBuilder();
  protected readonly contactForm = this.fb.nonNullable.group({
    name: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    subject: ['', [Validators.required]],
    message: ['', [Validators.required]],
  });

  protected readonly sent = signal(false);

  protected toggleFaq(i: number): void {
    this.openFaq.set(this.openFaq() === i ? null : i);
  }

  protected send(): void {
    if (this.contactForm.invalid) {
      this.contactForm.markAllAsTouched();
      return;
    }
    this.sent.set(true);
    this.contactForm.reset();
  }
}
