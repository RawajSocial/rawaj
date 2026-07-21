import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PageHeader } from '../../../../shared/components/page-header/page-header';
import { UsersApiService } from '../../../../core/api/users-api.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { ApiError } from '../../../../core/api';
import { Language } from '../../../../core/models';

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
  private readonly usersApi = inject(UsersApiService);
  private readonly authService = inject(AuthService);

  protected readonly tab = signal<SettingsTab>('profile');

  protected readonly languages: { value: Language; label: string }[] = [
    { value: 'Ar', label: 'العربية' },
    { value: 'En', label: 'English' },
  ];

  private readonly fb = new FormBuilder();
  protected readonly profileForm = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.maxLength(200)]],
    email: [{ value: '', disabled: true }],
    preferredLanguage: ['Ar' as Language, [Validators.required]],
    avatarUrl: [''],
  });

  protected readonly loadingProfile = signal(true);
  protected readonly savingProfile = signal(false);
  protected readonly profileSaved = signal(false);
  protected readonly profileError = signal<string | null>(null);

  protected readonly passwordForm = this.fb.nonNullable.group({
    currentPassword: ['', [Validators.required]],
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', [Validators.required]],
  });
  protected readonly changingPassword = signal(false);
  protected readonly passwordSaved = signal(false);
  protected readonly passwordError = signal<string | null>(null);

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

  constructor() {
    this.usersApi.getMyProfile().subscribe({
      next: (profile) => {
        this.profileForm.reset({
          fullName: profile.fullName,
          email: profile.email,
          preferredLanguage: profile.preferredLanguage,
          avatarUrl: profile.avatarUrl ?? '',
        });
        this.loadingProfile.set(false);
      },
      error: (error: unknown) => {
        this.loadingProfile.set(false);
        this.profileError.set(error instanceof ApiError ? error.message : 'تعذر تحميل بيانات الملف الشخصي.');
      },
    });
  }

  protected setTab(t: SettingsTab): void {
    this.tab.set(t);
  }

  protected initials(): string {
    const name = this.profileForm.getRawValue().fullName.trim();
    if (!name) return '؟';
    return name.split(/\s+/).slice(0, 2).map((p) => p.charAt(0)).join('');
  }

  protected togglePreference(key: string): void {
    this.preferences.update(list => list.map(p => (p.key === key ? { ...p, enabled: !p.enabled } : p)));
  }

  protected saveProfile(): void {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }

    const { fullName, preferredLanguage, avatarUrl } = this.profileForm.getRawValue();

    this.savingProfile.set(true);
    this.profileSaved.set(false);
    this.profileError.set(null);

    this.usersApi.updateMyProfile({ fullName, preferredLanguage, avatarUrl: avatarUrl || null }).subscribe({
      next: () => {
        this.savingProfile.set(false);
        this.profileSaved.set(true);
        this.authService.updateCurrentUserName(fullName);
        setTimeout(() => this.profileSaved.set(false), 3000);
      },
      error: (error: unknown) => {
        this.savingProfile.set(false);
        this.profileError.set(error instanceof ApiError ? error.message : 'تعذر حفظ التغييرات.');
      },
    });
  }

  protected changePassword(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    const { currentPassword, newPassword, confirmPassword } = this.passwordForm.getRawValue();
    if (newPassword !== confirmPassword) {
      this.passwordError.set('كلمتا المرور غير متطابقتين.');
      return;
    }

    this.changingPassword.set(true);
    this.passwordSaved.set(false);
    this.passwordError.set(null);

    this.usersApi.changeMyPassword({ currentPassword, newPassword }).subscribe({
      next: () => {
        this.changingPassword.set(false);
        this.passwordSaved.set(true);
        this.passwordForm.reset({ currentPassword: '', newPassword: '', confirmPassword: '' });
        setTimeout(() => this.passwordSaved.set(false), 3000);
      },
      error: (error: unknown) => {
        this.changingPassword.set(false);
        this.passwordError.set(error instanceof ApiError ? error.message : 'تعذر تغيير كلمة المرور.');
      },
    });
  }
}
