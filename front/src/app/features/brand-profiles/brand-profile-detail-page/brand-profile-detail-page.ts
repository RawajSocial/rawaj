import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { of, switchMap, throwError } from 'rxjs';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { FileUpload } from '../../../shared/components/file-upload/file-upload';
import { BrandProfileService } from '../../../services/brand-profile.service';
import { TenantService } from '../../../core/tenant/tenant.service';
import { BrandProfileDetail, BrandVoice, BRAND_PROFILE_STATUS_LABELS, BRAND_VOICE_LABELS } from '../../../model/brand-profile.model';
import { SeoService } from '../../../services/seo.service';
import { ErrorModalService } from '../../../services/error-modal.service';
import { LoaderService } from '../../../services/loader.service';
import { extractApiErrorMessage } from '../../../core/auth/api-error.util';

type ChipOption = { value: string; label: string; icon: string };

const TONE_OPTIONS: { value: BrandVoice; label: string }[] = (
  Object.entries(BRAND_VOICE_LABELS) as [BrandVoice, string][]
).map(([value, label]) => ({ value, label }));

const STAGE_OPTIONS: ChipOption[] = [
  { value: 'إطلاق جديد', label: 'إطلاق جديد', icon: 'fa-solid fa-plus' },
  { value: 'نمو وتسويق', label: 'نمو وتسويق', icon: 'fa-solid fa-arrow-trend-up' },
  { value: 'توسع وتطوير', label: 'توسع وتطوير', icon: 'fa-solid fa-rocket' },
  { value: 'إعادة تموضع', label: 'إعادة تموضع', icon: 'fa-solid fa-rotate' },
];

const PRICE_OPTIONS: ChipOption[] = [
  { value: 'budget', label: 'اقتصادي', icon: 'fa-solid fa-coins' },
  { value: 'mid', label: 'متوسط', icon: 'fa-solid fa-scale-balanced' },
  { value: 'premium', label: 'راقٍ', icon: 'fa-solid fa-gem' },
  { value: 'luxury', label: 'فاخر', icon: 'fa-solid fa-crown' },
];

const STORE_OPTIONS: ChipOption[] = [
  { value: 'online', label: 'إلكتروني فقط', icon: 'fa-solid fa-globe' },
  { value: 'physical', label: 'متجر فعلي', icon: 'fa-solid fa-store' },
  { value: 'both', label: 'إلكتروني وفعلي', icon: 'fa-solid fa-shop' },
];

const PLATFORM_OPTIONS: ChipOption[] = [
  { value: 'instagram', label: 'Instagram', icon: 'fa-brands fa-instagram' },
  { value: 'facebook', label: 'Facebook', icon: 'fa-brands fa-facebook' },
];

const BUSINESS_AGE_OPTIONS = ['أقل من 6 أشهر', '6 - 12 شهرًا', '1 - 3 سنوات', '3 - 5 سنوات', 'أكثر من 5 سنوات'];

/** Turns a comma-separated field back into a clean string[] for the update request; `undefined`
 *  (rather than `[]`) when the field was left blank so the partial-update backend leaves the
 *  existing value untouched instead of wiping it. */
function parseList(raw: string): string[] | undefined {
  const items = raw.split(',').map(s => s.trim()).filter(Boolean);
  return items.length > 0 ? items : undefined;
}

@Component({
  selector: 'app-brand-profile-detail-page',
  imports: [RouterLink, ReactiveFormsModule, PageHeader, FileUpload],
  templateUrl: './brand-profile-detail-page.html',
  styleUrls: ['../../dashboard/dashboard-shared.css', './brand-profile-detail-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BrandProfileDetailPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly brandProfileService = inject(BrandProfileService);
  private readonly tenantService = inject(TenantService);
  private readonly seo = inject(SeoService);
  private readonly errorModalService = inject(ErrorModalService);
  private readonly loaderService = inject(LoaderService);

  protected readonly statusLabels = BRAND_PROFILE_STATUS_LABELS;
  protected readonly toneOptions = TONE_OPTIONS;
  protected readonly stageOptions = STAGE_OPTIONS;
  protected readonly priceOptions = PRICE_OPTIONS;
  protected readonly storeOptions = STORE_OPTIONS;
  protected readonly platformOptions = PLATFORM_OPTIONS;
  protected readonly businessAgeOptions = BUSINESS_AGE_OPTIONS;

  protected readonly brandProfileId = this.route.snapshot.paramMap.get('id') ?? '';
  protected readonly loading = signal(true);
  protected readonly notFound = signal(false);
  protected readonly detail = signal<BrandProfileDetail | null>(null);
  protected readonly saving = signal(false);

  /** Only Editor and above may modify a brand profile (matches the backend's MinimumRole on
   *  UpdateBrandProfileCommand) — Viewer gets a read-only page. Owner/Admin always pass regardless
   *  of TenantMemberBrandAccess; Editor additionally needs to have been granted this specific
   *  brand (enforced server-side — the page just reflects the role-level part of that rule). */
  protected readonly canEdit = computed(() => {
    const role = this.tenantService.tenant()?.role;
    return role === 'Owner' || role === 'Admin' || role === 'Editor';
  });

  private readonly fb = new FormBuilder();
  protected readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    tagline: [''],
    industry: [''],
    targetAudience: [''],
    description: [''],
    websiteUrl: [''],
    location: [''],
    colors: [''],
    supportedLanguages: [''],
    keywords: [''],
    instagram: [''],
    businessAge: [''],
    businessEstablishDate: [''],
    uniqueValue: [''],
    admiredBrand1: [''],
    admiredBrand2: [''],
    admiredBrand3: [''],
  });

  protected readonly selectedTones = signal<BrandVoice[]>([]);
  protected readonly selectedStage = signal<string | null>(null);
  protected readonly selectedPricePositioning = signal<string | null>(null);
  protected readonly selectedStorePresence = signal<string | null>(null);
  protected readonly selectedPlatforms = signal<string[]>([]);

  protected readonly logoDataUrl = signal<string | null>(null);
  private readonly logoFile = signal<File | null>(null);

  constructor() {
    this.seo.setPageSeo({
      title: 'ملف العلامة التجارية | رواج',
      description: 'اطّلع على بيانات العلامة التجارية وعدّلها.',
      keywords: 'رواج, العلامة التجارية, ملف العلامة',
      path: `/dashboard/brand-profiles/${this.brandProfileId}`,
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });

    if (!this.brandProfileId) {
      this.loading.set(false);
      this.notFound.set(true);
      return;
    }

    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.brandProfileService.getDetail(this.brandProfileId).subscribe({
      next: res => {
        this.loading.set(false);
        if (!res.data) {
          this.notFound.set(true);
          return;
        }
        this.detail.set(res.data);
        this.form.reset({
          name: res.data.name,
          tagline: res.data.tagline ?? '',
          industry: res.data.industry ?? '',
          targetAudience: res.data.targetAudience ?? '',
          description: res.data.description ?? '',
          websiteUrl: res.data.websiteUrl ?? '',
          location: res.data.location ?? '',
          colors: res.data.colors.join(', '),
          supportedLanguages: res.data.supportedLanguages.join(', '),
          keywords: res.data.keywords.join(', '),
          instagram: res.data.instagram ?? '',
          businessAge: res.data.businessAge ?? '',
          businessEstablishDate: res.data.businessEstablishDate ?? '',
          uniqueValue: res.data.uniqueValue ?? '',
          admiredBrand1: res.data.admiredBrand1 ?? '',
          admiredBrand2: res.data.admiredBrand2 ?? '',
          admiredBrand3: res.data.admiredBrand3 ?? '',
        });
        this.selectedTones.set(res.data.tones);
        this.selectedStage.set(res.data.stage ?? null);
        this.selectedPricePositioning.set(res.data.pricePositioning ?? null);
        this.selectedStorePresence.set(res.data.storePresence ?? null);
        this.selectedPlatforms.set(res.data.existingPlatforms);
        this.logoDataUrl.set(res.data.logoUrl ?? null);
      },
      error: () => {
        this.loading.set(false);
        this.notFound.set(true);
      },
    });
  }

  protected toggleTone(tone: BrandVoice): void {
    this.selectedTones.update(current => {
      const idx = current.indexOf(tone);
      if (idx >= 0) return current.filter(t => t !== tone);
      if (current.length >= 5) return current;
      return [...current, tone];
    });
  }

  protected selectStage(value: string): void {
    this.selectedStage.set(this.selectedStage() === value ? null : value);
  }

  protected selectPricePositioning(value: string): void {
    this.selectedPricePositioning.set(this.selectedPricePositioning() === value ? null : value);
  }

  protected selectStorePresence(value: string): void {
    this.selectedStorePresence.set(this.selectedStorePresence() === value ? null : value);
  }

  protected togglePlatform(value: string): void {
    this.selectedPlatforms.update(current =>
      current.includes(value) ? current.filter(p => p !== value) : [...current, value],
    );
  }

  protected onLogoSelected(file: File): void {
    this.logoFile.set(file);
    const reader = new FileReader();
    reader.onload = e => this.logoDataUrl.set(e.target?.result as string);
    reader.readAsDataURL(file);
  }

  protected onLogoCleared(): void {
    this.logoFile.set(null);
    this.logoDataUrl.set(this.detail()?.logoUrl ?? null);
  }

  protected save(): void {
    if (!this.canEdit() || this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    const {
      name, tagline, industry, targetAudience, description, websiteUrl, location, colors, supportedLanguages,
      keywords, instagram, businessAge, businessEstablishDate, uniqueValue,
      admiredBrand1, admiredBrand2, admiredBrand3,
    } = this.form.getRawValue();

    const file = this.logoFile();
    const logoUrl$ = file
      ? this.brandProfileService.uploadLogo(file).pipe(
          switchMap(res =>
            res.status === 'success' && res.data
              ? of<string | undefined>(res.data.logoUrl)
              : throwError(() => new Error(res.message ?? 'تعذّر رفع الشعار.')),
          ),
        )
      : of<string | undefined>(undefined);

    this.saving.set(true);
    this.loaderService.show();
    logoUrl$
      .pipe(
        switchMap(logoUrl =>
          this.brandProfileService.update(this.brandProfileId, {
            name,
            tagline: tagline || undefined,
            industry: industry || undefined,
            targetAudience: targetAudience || undefined,
            description: description || undefined,
            websiteUrl: websiteUrl || undefined,
            location: location || undefined,
            tones: this.selectedTones(),
            colors: parseList(colors),
            supportedLanguages: parseList(supportedLanguages),
            keywords: parseList(keywords),
            instagram: instagram || undefined,
            businessAge: businessAge || undefined,
            businessEstablishDate: businessEstablishDate || undefined,
            stage: this.selectedStage() ?? undefined,
            uniqueValue: uniqueValue || undefined,
            pricePositioning: this.selectedPricePositioning() ?? undefined,
            storePresence: this.selectedStorePresence() ?? undefined,
            existingPlatforms: this.selectedPlatforms(),
            admiredBrand1: admiredBrand1 || undefined,
            admiredBrand2: admiredBrand2 || undefined,
            admiredBrand3: admiredBrand3 || undefined,
            logoUrl,
          }),
        ),
      )
      .subscribe({
        next: res => {
          this.saving.set(false);
          this.loaderService.hide();
          if (res.status !== 'success' || !res.data) {
            this.errorModalService.show(res.message ?? 'تعذّر حفظ ملف العلامة التجارية.', { variant: 'error' });
            return;
          }
          this.logoFile.set(null);
          this.errorModalService.show('تم حفظ ملف العلامة التجارية بنجاح.', { variant: 'success' });
          this.load();
        },
        error: err => {
          this.saving.set(false);
          this.loaderService.hide();
          this.errorModalService.show(
            extractApiErrorMessage(err, 'تعذّر حفظ ملف العلامة التجارية. يرجى المحاولة مرة أخرى.'),
            { variant: 'error' },
          );
        },
      });
  }

  protected cancel(): void {
    this.router.navigate(['/dashboard/brand-profiles']);
  }
}
