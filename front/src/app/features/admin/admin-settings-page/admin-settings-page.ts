import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { SeoService } from '../../../services/seo.service';

interface PlatformToggle {
  key: string;
  title: string;
  desc: string;
  enabled: boolean;
}

@Component({
  selector: 'app-admin-settings-page',
  imports: [],
  templateUrl: './admin-settings-page.html',
  styleUrls: ['../../dashboard/dashboard-shared.css', '../admin-shared.css', './admin-settings-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminSettingsPage {
  private readonly seo = inject(SeoService);

  protected readonly toggles = signal<PlatformToggle[]>([
    { key: 'maintenance',   title: 'وضع الصيانة',              desc: 'تعطيل الوصول إلى المنصة مؤقتًا لجميع المستخدمين باستثناء المدراء.', enabled: false },
    { key: 'signups',       title: 'السماح بتسجيل حسابات جديدة', desc: 'السماح للزوار بإنشاء حسابات جديدة على المنصة.',                     enabled: true },
    { key: 'trials',        title: 'تفعيل الفترة التجريبية',    desc: 'منح الحسابات الجديدة فترة تجريبية مجانية قبل الاشتراك.',            enabled: true },
    { key: 'notifications', title: 'إشعارات النظام العامة',     desc: 'إرسال إشعارات جماعية للمستخدمين عند وجود تحديثات مهمة.',            enabled: true },
  ]);

  constructor() {
    this.seo.setPageSeo({
      title: 'الإعدادات العامة | إدارة المنصة | رواج',
      description: 'إعدادات عامة للتحكم في سلوك منصة رواج.',
      keywords: 'رواج, إدارة المنصة, الإعدادات العامة',
      path: '/admin/settings',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });
  }

  protected toggle(key: string): void {
    this.toggles.update(list => list.map(t => (t.key === key ? { ...t, enabled: !t.enabled } : t)));
  }
}
