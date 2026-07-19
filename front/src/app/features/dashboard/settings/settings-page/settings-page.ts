import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PageHeader } from '../../../../shared/components/page-header/page-header';
import { SeoService } from '../../../../services/seo.service';
import { TenantService } from '../../../../services/tenant.service';
import { BrandProfileService } from '../../../../services/brand-profile.service';

type SettingsTab = 'profile' | 'preferences' | 'connections';

interface Preference {
  key: string;
  title: string;
  desc: string;
  enabled: boolean;
}

interface ConnectPlatform {
  key: string;
  label: string;
  icon: string;
  color: string;
}

const CONNECT_PLATFORMS: ConnectPlatform[] = [
  { key: 'instagram', label: 'إنستغرام',  icon: 'fa-brands fa-instagram',   color: 'var(--color-instagram)' },
  { key: 'facebook',  label: 'فيسبوك',    icon: 'fa-brands fa-facebook-f',  color: 'var(--color-facebook)' },
  { key: 'tiktok',    label: 'تيك توك',   icon: 'fa-brands fa-tiktok',      color: 'var(--gradient-tiktok)' },
  { key: 'snapchat',  label: 'سناب شات',  icon: 'fa-brands fa-snapchat',    color: 'var(--color-snapchat)' },
  { key: 'linkedin',  label: 'لينكدإن',   icon: 'fa-brands fa-linkedin-in', color: 'var(--color-linkedin)' },
  { key: 'youtube',   label: 'يوتيوب',    icon: 'fa-brands fa-youtube',     color: 'var(--color-youtube)' },
];

@Component({
  selector: 'app-settings-page',
  imports: [PageHeader, ReactiveFormsModule],
  templateUrl: './settings-page.html',
  styleUrls: ['../../dashboard-shared.css', './settings-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SettingsPage {
  private readonly seo = inject(SeoService);
  private readonly tenantService = inject(TenantService);
  private readonly brandProfileService = inject(BrandProfileService);

  protected readonly isAgency = this.tenantService.isAgency;
  protected readonly brandProfiles = this.brandProfileService.profiles;
  protected readonly connectPlatforms = CONNECT_PLATFORMS;

  constructor() {
    this.seo.setPageSeo({
      title: 'الإعدادات | رواج',
      description: 'تحكم في إعدادات حسابك وتفضيلاتك.',
      keywords: 'رواج, الإعدادات, تفضيلات الحساب',
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

  // TODO: replace with the real OAuth connect/disconnect flow once the
  // backend endpoints exist — for now this only flips local UI state.
  // Keyed by brand profile id so an agency's clients each keep their own
  // connected accounts; a single-business tenant only ever uses one key.
  private readonly connectionsByBrand = signal<Map<string, Set<string>>>(
    new Map([['bp1', new Set(['instagram', 'facebook'])]]),
  );

  protected readonly selectedBrandProfileId = signal<string>('bp1');

  protected readonly activeBrandProfile = computed(() =>
    this.brandProfiles().find(p => p.id === this.selectedBrandProfileId()),
  );

  protected selectBrandProfile(id: string): void {
    this.selectedBrandProfileId.set(id);
  }

  protected isConnected(platformKey: string): boolean {
    const brandId = this.isAgency() ? this.selectedBrandProfileId() : 'default';
    return this.connectionsByBrand().get(brandId)?.has(platformKey) ?? false;
  }

  protected toggleConnect(platformKey: string): void {
    const brandId = this.isAgency() ? this.selectedBrandProfileId() : 'default';
    this.connectionsByBrand.update(map => {
      const next = new Map(map);
      const current = new Set(next.get(brandId) ?? []);
      current.has(platformKey) ? current.delete(platformKey) : current.add(platformKey);
      next.set(brandId, current);
      return next;
    });
  }

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
