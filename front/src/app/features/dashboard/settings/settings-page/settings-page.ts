import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';
import { PageHeader } from '../../../../shared/components/page-header/page-header';
import { FileUpload } from '../../../../shared/components/file-upload/file-upload';
import { SeoService } from '../../../../services/seo.service';
import { TenantService } from '../../../../core/tenant/tenant.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { BrandProfileService } from '../../../../services/brand-profile.service';
import { ErrorModalService } from '../../../../services/error-modal.service';
import { ConfirmDialogService } from '../../../../services/confirm-dialog.service';
import { LoaderService } from '../../../../services/loader.service';
import { FormErrorsService } from '../../../../services/form-errors.service';
import { extractApiErrorMessage, applyFieldErrors } from '../../../../core/auth/api-error.util';
import { usernameValidators } from '../../../../core/auth/username.validators';
import { UpdateMyProfileRequest } from '../../../../model/auth.model';

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
  imports: [PageHeader, ReactiveFormsModule, FileUpload],
  templateUrl: './settings-page.html',
  styleUrls: ['../../dashboard-shared.css', './settings-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SettingsPage {
  private readonly seo = inject(SeoService);
  private readonly tenantService = inject(TenantService);
  private readonly authService = inject(AuthService);
  private readonly brandProfileService = inject(BrandProfileService);
  private readonly errorModalService = inject(ErrorModalService);
  private readonly confirmDialogService = inject(ConfirmDialogService);
  private readonly loaderService = inject(LoaderService);
  private readonly formErrorsService = inject(FormErrorsService);

  protected readonly currentUser = this.authService.currentUser;
  protected readonly profileLoaded = computed(() => !!this.currentUser());

  protected readonly isAgency = this.tenantService.isAgency;
  protected readonly isActivated = this.tenantService.isActivated;
  protected readonly coinBalance = this.tenantService.coinBalance;
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

    effect(() => {
      const tenant = this.tenantService.tenant();
      if (!tenant) return;
      this.businessForm.patchValue(
        {
          phone: tenant.phone ?? '',
          industry: tenant.industry ?? '',
          country: tenant.country ?? '',
          city: tenant.city ?? '',
          website: tenant.website ?? '',
        },
        { emitEvent: false },
      );
    });

    effect(() => {
      const user = this.currentUser();
      if (!user) return;
      this.profileForm.patchValue(
        { fullName: user.fullName, username: user.username, preferredLanguage: user.preferredLanguage, email: user.email },
        { emitEvent: false },
      );
      this.profileForm.markAsPristine();
    });

    this.profileForm.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.formTick.update(n => n + 1));
  }

  private readonly formTick = signal(0);
  protected readonly canSaveProfile = computed(() => {
    this.formTick();
    return this.profileForm.dirty && this.profileForm.valid && !this.profileSaving();
  });

  protected readonly tab = signal<SettingsTab>('profile');

  private readonly fb = new FormBuilder();
  protected readonly profileSubmitted = signal(false);
  protected readonly profileSaving = signal(false);
  protected readonly profileForm = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.maxLength(200)]],
    username: ['', usernameValidators],
    preferredLanguage: ['Ar' as 'Ar' | 'En', [Validators.required]],
    email: [{ value: '', disabled: true }],
  });

  protected readonly businessForm = this.fb.nonNullable.group({
    phone: [''],
    industry: [''],
    country: [''],
    city: [''],
    website: [''],
  });

  protected readonly passwordSubmitted = signal(false);
  protected readonly passwordForm = this.fb.nonNullable.group({
    currentPassword: ['', [Validators.required]],
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', [Validators.required, Validators.minLength(8)]],
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

  protected profileErrorMessage(controlName: 'fullName' | 'username'): string | null {
    const messages = {
      fullName: { required: 'الاسم الكامل مطلوب.', maxlength: 'الاسم الكامل يجب ألا يتجاوز 200 حرف.' },
      username: {
        required: 'اسم المستخدم مطلوب.',
        minlength: 'اسم المستخدم يجب أن يكون 3 أحرف على الأقل.',
        maxlength: 'اسم المستخدم يجب ألا يتجاوز 30 حرفًا.',
        pattern: 'اسم المستخدم يجب أن يبدأ بحرف ويحتوي على أحرف/أرقام/underscore فقط.',
      },
    } as const;
    return this.formErrorsService.getControlErrorMessage(
      this.profileForm.controls[controlName],
      this.profileSubmitted(),
      messages[controlName],
    );
  }

  protected togglePreference(key: string): void {
    this.preferences.update(list => list.map(p => (p.key === key ? { ...p, enabled: !p.enabled } : p)));
  }

  protected saveProfile(): void {
    this.profileSubmitted.set(true);
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }
    if (this.profileSaving()) {
      return;
    }

    const current = this.currentUser();
    const { fullName, username, preferredLanguage } = this.profileForm.getRawValue();
    const changes: UpdateMyProfileRequest = {};
    if (fullName !== current?.fullName) changes.fullName = fullName;
    if (username !== current?.username) changes.username = username;
    if (preferredLanguage !== current?.preferredLanguage) changes.preferredLanguage = preferredLanguage;

    if (Object.keys(changes).length === 0) {
      this.errorModalService.show('لا توجد تغييرات لحفظها.', { variant: 'info' });
      return;
    }

    this.profileSaving.set(true);
    this.authService.updateMyProfile(changes).subscribe({
      next: res => {
        this.profileSaving.set(false);
        if (res.status !== 'success' || !res.data) {
          this.errorModalService.show(res.message ?? 'تعذّر حفظ الملف الشخصي.', { variant: 'error' });
          return;
        }
        this.profileForm.markAsPristine();
        this.errorModalService.show('تم حفظ الملف الشخصي بنجاح.', { variant: 'success' });
      },
      error: err => {
        this.profileSaving.set(false);
        applyFieldErrors(this.profileForm, err);
        this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر حفظ الملف الشخصي. يرجى المحاولة مرة أخرى.'), {
          variant: 'error',
        });
      },
    });
  }

  protected readonly avatarUpload = viewChild.required<FileUpload>('avatarUpload');
  protected readonly avatarUploading = signal(false);
  protected readonly avatarUploadProgress = signal<number | null>(null);
  protected readonly avatarValidationError = signal<string | null>(null);
  private avatarUploadSub?: Subscription;

  protected onAvatarSelected(file: File): void {
    this.avatarValidationError.set(null);
    this.avatarUploading.set(true);
    this.avatarUploadProgress.set(0);

    this.avatarUploadSub = this.authService.uploadAvatar(file, pct => this.avatarUploadProgress.set(pct)).subscribe({
      next: res => {
        this.avatarUploading.set(false);
        this.avatarUploadProgress.set(null);
        if (res.status !== 'success' || !res.data) {
          this.avatarUpload().reset();
          this.errorModalService.show(res.message ?? 'تعذّر رفع الصورة.', { variant: 'error' });
        }
      },
      error: err => {
        this.avatarUploading.set(false);
        this.avatarUploadProgress.set(null);
        this.avatarUpload().reset();
        this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر رفع الصورة. يرجى المحاولة مرة أخرى.'), {
          variant: 'error',
        });
      },
    });
  }

  protected cancelAvatarUpload(): void {
    this.avatarUploadSub?.unsubscribe();
    this.avatarUploading.set(false);
    this.avatarUploadProgress.set(null);
    this.avatarUpload().reset();
  }

  protected onAvatarValidationError(message: string): void {
    this.avatarValidationError.set(message);
  }

  protected readonly avatarDeleting = signal(false);

  protected async onAvatarRemoved(): Promise<void> {
    if (this.avatarDeleting() || this.avatarUploading()) return;

    const confirmed = await this.confirmDialogService.confirm(
      'هل أنت متأكد من حذف صورة الملف الشخصي؟ لا يمكن التراجع عن هذا الإجراء.',
      { title: 'حذف الصورة', confirmLabel: 'حذف', variant: 'danger' },
    );
    if (!confirmed) return;

    this.avatarDeleting.set(true);
    this.authService.deleteAvatar().subscribe({
      next: res => {
        this.avatarDeleting.set(false);
        if (res.status !== 'success') {
          this.errorModalService.show(res.message ?? 'تعذّر حذف الصورة.', { variant: 'error' });
        }
      },
      error: err => {
        this.avatarDeleting.set(false);
        this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر حذف الصورة. يرجى المحاولة مرة أخرى.'), {
          variant: 'error',
        });
      },
    });
  }

  protected passwordErrorMessage(controlName: 'currentPassword' | 'newPassword' | 'confirmPassword'): string | null {
    const messages: Record<string, { required?: string; minlength?: string }> = {
      currentPassword: { required: 'كلمة المرور الحالية مطلوبة.' },
      newPassword: {
        required: 'كلمة المرور الجديدة مطلوبة.',
        minlength: 'كلمة المرور الجديدة يجب أن تكون 8 أحرف على الأقل.',
      },
      confirmPassword: {
        required: 'تأكيد كلمة المرور مطلوب.',
        minlength: 'تأكيد كلمة المرور يجب أن يكون 8 أحرف على الأقل.',
      },
    };
    return this.formErrorsService.getControlErrorMessage(
      this.passwordForm.controls[controlName],
      this.passwordSubmitted(),
      messages[controlName],
    );
  }

  protected passwordsMismatchMessage(): string | null {
    const { newPassword, confirmPassword } = this.passwordForm.getRawValue();
    return this.formErrorsService.getPasswordsMismatchMessage(newPassword, confirmPassword, this.passwordSubmitted());
  }

  protected changePassword(): void {
    this.passwordSubmitted.set(true);
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    const { currentPassword, newPassword, confirmPassword } = this.passwordForm.getRawValue();
    if (newPassword !== confirmPassword) {
      return;
    }

    this.loaderService.show();
    this.authService.changePassword({ currentPassword, newPassword }).subscribe({
      next: res => {
        this.loaderService.hide();
        if (res.status !== 'success') {
          this.errorModalService.show(res.message ?? 'تعذّر تغيير كلمة المرور.', { variant: 'error' });
          return;
        }
        this.passwordForm.reset();
        this.passwordSubmitted.set(false);
        this.errorModalService.show('تم تغيير كلمة المرور بنجاح.', { variant: 'success' });
      },
      error: err => {
        this.loaderService.hide();
        applyFieldErrors(this.passwordForm, err);
        this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر تغيير كلمة المرور. يرجى المحاولة مرة أخرى.'), {
          variant: 'error',
        });
      },
    });
  }

  protected saveBusinessProfile(): void {
    const wasActivated = this.isActivated();
    this.loaderService.show();
    this.tenantService.updateProfile(this.businessForm.getRawValue()).subscribe({
      next: res => {
        this.loaderService.hide();
        if (res.status === 'success' && res.data?.activationRewardGranted && !wasActivated) {
          this.errorModalService.show('تم تفعيل حسابك بنجاح! حصلت على 50 كوين مكافأة.', {
            variant: 'success',
            title: 'تم تفعيل الحساب',
          });
        } else if (res.status !== 'success') {
          this.errorModalService.show(res.message ?? 'تعذّر حفظ بيانات النشاط.', { variant: 'error' });
        }
      },
      error: () => {
        this.loaderService.hide();
        this.errorModalService.show('تعذّر حفظ بيانات النشاط. يرجى المحاولة مرة أخرى.', { variant: 'error' });
      },
    });
  }
}
