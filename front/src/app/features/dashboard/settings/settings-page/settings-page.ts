import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PageHeader } from '../../../../shared/components/page-header/page-header';
import { SeoService } from '../../../../services/seo.service';

type SettingsTab = 'profile' | 'preferences' | 'appearance';

interface Preference {
  key: string;
  title: string;
  desc: string;
  enabled: boolean;
}

@Component({
  selector: 'app-settings-page',
  imports: [PageHeader, ReactiveFormsModule],
  templateUrl: './settings-page.html',
  styleUrls: ['../../dashboard-shared.css', './settings-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SettingsPage {
  private readonly seo = inject(SeoService);

  constructor() {
    this.seo.setPageSeo({
      title: 'الإعدادات | رواج',
      description: 'تحكم في إعدادات حسابك وتفضيلاتك ومظهر لوحة التحكم.',
      keywords: 'رواج, الإعدادات, تفضيلات الحساب, المظهر',
      path: '/dashboard/settings',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });
  }

  protected readonly tab = signal<SettingsTab>('profile');

  private readonly fb = new FormBuilder();
  protected readonly profileForm = this.fb.nonNullable.group({
    firstName: ['هيثم', [Validators.required]],
    lastName: ['أحمد', [Validators.required]],
    email: ['haitham@rawaj.com', [Validators.required, Validators.email]],
    bio: ['مؤسس وكالة رواج للتسويق الرقمي.'],
  });

  protected readonly preferences = signal<Preference[]>([
    { key: 'campaign', title: 'إشعارات الحملات', desc: 'استقبل تنبيهات عند نشر أو تغيّر حالة الحملات.', enabled: true },
    { key: 'content', title: 'إشعارات المحتوى', desc: 'تنبيه عند اكتمال توليد المحتوى بالذكاء الاصطناعي.', enabled: true },
    { key: 'team', title: 'نشاط الفريق', desc: 'إشعارات عند انضمام أعضاء أو تغيير صلاحياتهم.', enabled: false },
    { key: 'billing', title: 'تنبيهات الفوترة', desc: 'تذكيرات بمواعيد التجديد والفواتير.', enabled: true },
    { key: 'digest', title: 'الملخص الأسبوعي', desc: 'ملخص أداء حساباتك مرة كل أسبوع عبر البريد.', enabled: false },
  ]);

  protected readonly themes: { value: string; label: string; icon: string }[] = [
    { value: 'light', label: 'فاتح', icon: 'fa-sun' },
    { value: 'dark', label: 'داكن', icon: 'fa-moon' },
    { value: 'system', label: 'حسب النظام', icon: 'fa-desktop' },
  ];
  protected readonly activeTheme = signal('light');

  protected readonly accents: string[] = ['#7C3AED', '#2563EB', '#16A34A', '#F97316', '#EC4899'];
  protected readonly activeAccent = signal('#7C3AED');

  protected setTab(t: SettingsTab): void { this.tab.set(t); }

  protected initials(): string {
    const { firstName, lastName } = this.profileForm.getRawValue();
    return (firstName.charAt(0) + lastName.charAt(0)) || '؟';
  }

  protected togglePreference(key: string): void {
    this.preferences.update(list => list.map(p => (p.key === key ? { ...p, enabled: !p.enabled } : p)));
  }

  protected saveProfile(): void {
    this.profileForm.markAllAsTouched();
  }
}
