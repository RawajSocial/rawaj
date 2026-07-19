import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { PageHeader } from '../../../../shared/components/page-header/page-header';
import { SeoService } from '../../../../services/seo.service';
import { TooltipDirective } from '../../../../shared/directives/tooltip.directive';

interface UsageMetric {
  label: string;
  used: number;
  limit: number;
  unit: string;
}

interface Invoice {
  date: string;
  desc: string;
  amount: string;
  status: 'paid' | 'pending';
}

@Component({
  selector: 'app-billing-page',
  imports: [PageHeader, TooltipDirective],
  templateUrl: './billing-page.html',
  styleUrls: ['../../dashboard-shared.css', './billing-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BillingPage {
  private readonly seo = inject(SeoService);

  constructor() {
    this.seo.setPageSeo({
      title: 'الفوترة والاشتراك | رواج',
      description: 'تابع باقتك واستهلاكك وفواتيرك السابقة.',
      keywords: 'رواج, الفوترة, الاشتراك, الباقات, الفواتير',
      path: '/dashboard/billing',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });
  }

  protected readonly planFeatures = [
    'إنشاء غير محدود للتصميمات',
    'ربط جميع منصات التواصل',
    'تحليلات وتقارير متقدمة',
    'دعم فني ذو أولوية',
    'دعوة حتى 10 أعضاء للفريق',
  ];

  protected readonly usage = signal<UsageMetric[]>([
    { label: 'نقاط توليد المحتوى', used: 8420, limit: 10000, unit: 'نقطة' },
    { label: 'مساحة الوسائط', used: 3.2, limit: 5, unit: 'جيجابايت' },
    { label: 'أعضاء الفريق', used: 4, limit: 10, unit: 'عضو' },
  ]);

  protected readonly invoices = signal<Invoice[]>([
    { date: '١ فبراير ٢٠٢٦', desc: 'الباقة الأساسية — شهري', amount: '$15.00', status: 'paid' },
    { date: '١ يناير ٢٠٢٦', desc: 'الباقة الأساسية — شهري', amount: '$15.00', status: 'paid' },
    { date: '١ ديسمبر ٢٠٢٥', desc: 'الباقة الأساسية — شهري', amount: '$15.00', status: 'paid' },
    { date: '١ نوفمبر ٢٠٢٥', desc: 'الباقة المجانية — تجربة', amount: '$0.00', status: 'paid' },
  ]);

  protected percent(m: UsageMetric): number {
    return Math.min(100, Math.round((m.used / m.limit) * 100));
  }
}
