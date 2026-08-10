import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { of, switchMap, throwError } from 'rxjs';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { FileUpload } from '../../../shared/components/file-upload/file-upload';
import { BrandProfileService, CreateBrandProfileInput } from '../../../services/brand-profile.service';
import { BrandVoice } from '../../../model/brand-profile.model';
import { SeoService } from '../../../services/seo.service';
import { TenantService } from '../../../core/tenant/tenant.service';
import { ErrorModalService } from '../../../services/error-modal.service';
import { LoaderService } from '../../../services/loader.service';
import { extractApiErrorMessage } from '../../../core/auth/api-error.util';
import { isBrandLimitReached, promptBrandLimitUpgrade } from '../../../shared/utils/upgrade-prompts.util';

interface WizardStep {
  key: 'basics' | 'identity' | 'voice' | 'positioning' | 'logo' | 'review';
  label: string;
}

const STEPS: WizardStep[] = [
  { key: 'basics', label: 'الأساسيات' },
  { key: 'identity', label: 'الهوية' },
  { key: 'voice', label: 'أسلوب العلامة' },
  { key: 'positioning', label: 'التموضع' },
  { key: 'logo', label: 'الشعار' },
  { key: 'review', label: 'المراجعة' },
];

type ChipOption = { value: string; label: string; icon: string };

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

const LANGUAGE_OPTIONS: ChipOption[] = [
  { value: 'ar', label: 'العربية', icon: 'fa-solid fa-globe' },
  { value: 'en', label: 'الإنجليزية', icon: 'fa-solid fa-language' },
];

const BUSINESS_AGE_OPTIONS = ['أقل من 6 أشهر', '6 - 12 شهرًا', '1 - 3 سنوات', '3 - 5 سنوات', 'أكثر من 5 سنوات'];

@Component({
  selector: 'app-brand-profile-create-page',
  imports: [RouterLink, ReactiveFormsModule, PageHeader, FileUpload],
  templateUrl: './brand-profile-create-page.html',
  styleUrls: ['../../dashboard/dashboard-shared.css', './brand-profile-create-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BrandProfileCreatePage {
  private readonly brandProfileService = inject(BrandProfileService);
  private readonly tenantService = inject(TenantService);
  private readonly errorModalService = inject(ErrorModalService);
  private readonly loaderService = inject(LoaderService);
  private readonly router = inject(Router);
  private readonly seo = inject(SeoService);

  protected readonly steps = STEPS;
  protected readonly currentStepIndex = signal(0);
  protected readonly stageOptions = STAGE_OPTIONS;
  protected readonly priceOptions = PRICE_OPTIONS;
  protected readonly storeOptions = STORE_OPTIONS;
  protected readonly platformOptions = PLATFORM_OPTIONS;
  protected readonly languageOptions = LANGUAGE_OPTIONS;
  protected readonly businessAgeOptions = BUSINESS_AGE_OPTIONS;

  protected readonly progressPercent = () =>
    Math.round(((this.currentStepIndex() + 1) / this.steps.length) * 100);

  protected readonly voiceOptions: { value: BrandVoice; label: string; desc: string }[] = [
    { value: 'professional', label: 'احترافي', desc: 'نبرة رسمية وموثوقة' },
    { value: 'friendly', label: 'ودود', desc: 'نبرة قريبة ومرحّبة' },
    { value: 'bold', label: 'جريء', desc: 'نبرة واثقة ومباشرة' },
    { value: 'playful', label: 'مرح', desc: 'نبرة خفيفة ومرحة' },
    { value: 'elegant', label: 'أنيق', desc: 'نبرة راقية ومصقولة' },
    { value: 'inspiring', label: 'ملهم', desc: 'نبرة محفزة وطموحة' },
    { value: 'educational', label: 'تثقيفي', desc: 'نبرة تشرح وتفيد' },
    { value: 'innovative', label: 'مبتكر', desc: 'نبرة جريئة وحديثة' },
    { value: 'motivating', label: 'محفز', desc: 'نبرة دافعة للعمل' },
    { value: 'wittyFunny', label: 'ذكي ومضحك', desc: 'نبرة خفيفة الظل' },
  ];

  private readonly fb = new FormBuilder();
  protected readonly basicsForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    tagline: [''],
    industry: [''],
    description: [''],
    location: [''],
    website: [''],
  });

  protected readonly identityForm = this.fb.nonNullable.group({
    instagram: [''],
    businessAge: [''],
    businessEstablishDate: [''],
  });

  protected readonly positioningForm = this.fb.nonNullable.group({
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
  protected readonly selectedLanguages = signal<string[]>([]);
  protected readonly brandColors = signal<string[]>(['#7c3aed']);
  protected readonly maxColors = 5;
  private readonly colorPalette = ['#7c3aed', '#2563eb', '#16a34a', '#ea580c', '#db2777'];

  protected readonly logoDataUrl = signal<string | null>(null);
  protected readonly logoFileName = signal<string | null>(null);
  private readonly logoFile = signal<File | null>(null);

  constructor() {
    this.seo.setPageSeo({
      title: 'إنشاء ملف علامة تجارية | رواج',
      description: 'أنشئ ملف علامة تجارية جديدة خطوة بخطوة.',
      keywords: 'رواج, إنشاء علامة تجارية, ملف العلامة',
      path: '/dashboard/brand-profiles/new',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
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
    this.selectedStage.set(value);
  }

  protected selectPricePositioning(value: string): void {
    this.selectedPricePositioning.set(value);
  }

  protected selectStorePresence(value: string): void {
    this.selectedStorePresence.set(value);
  }

  protected togglePlatform(value: string): void {
    this.selectedPlatforms.update(current =>
      current.includes(value) ? current.filter(p => p !== value) : [...current, value],
    );
  }

  protected toggleLanguage(value: string): void {
    this.selectedLanguages.update(current =>
      current.includes(value) ? current.filter(l => l !== value) : [...current, value],
    );
  }

  protected setColor(index: number, value: string): void {
    this.brandColors.update(colors => colors.map((c, i) => (i === index ? value : c)));
  }

  protected toneLabel(tone: BrandVoice): string {
    return this.voiceOptions.find(o => o.value === tone)?.label ?? tone;
  }

  protected optionLabel(options: ChipOption[], value: string): string {
    return options.find(o => o.value === value)?.label ?? value;
  }

  protected setColorFromHex(index: number, raw: string): void {
    let hex = raw.trim();
    if (!hex.startsWith('#')) hex = '#' + hex;
    if (/^#[0-9a-fA-F]{3}$/.test(hex)) {
      hex = '#' + hex[1] + hex[1] + hex[2] + hex[2] + hex[3] + hex[3];
    }
    if (/^#[0-9a-fA-F]{6}$/.test(hex)) {
      this.setColor(index, hex.toLowerCase());
    }
  }

  protected addColor(): void {
    this.brandColors.update(colors => {
      if (colors.length >= this.maxColors) return colors;
      return [...colors, this.colorPalette[colors.length % this.colorPalette.length]];
    });
  }

  protected removeColor(index: number): void {
    this.brandColors.update(colors => colors.filter((_, i) => i !== index));
  }

  protected onLogoSelected(file: File): void {
    this.logoFileName.set(file.name);
    this.logoFile.set(file);
    // Data URL is used only for the in-wizard preview; the actual file is uploaded on submit.
    const reader = new FileReader();
    reader.onload = e => this.logoDataUrl.set(e.target?.result as string);
    reader.readAsDataURL(file);
  }

  protected onLogoCleared(): void {
    this.logoDataUrl.set(null);
    this.logoFileName.set(null);
    this.logoFile.set(null);
  }

  protected get isBasicsValid(): boolean {
    return this.basicsForm.valid;
  }

  protected next(): void {
    if (this.steps[this.currentStepIndex()].key === 'basics' && this.basicsForm.invalid) {
      this.basicsForm.markAllAsTouched();
      return;
    }
    if (this.currentStepIndex() < this.steps.length - 1) {
      this.currentStepIndex.update(i => i + 1);
    }
  }

  protected back(): void {
    if (this.currentStepIndex() > 0) {
      this.currentStepIndex.update(i => i - 1);
    }
  }

  protected goToStep(index: number): void {
    if (index <= this.currentStepIndex()) {
      this.currentStepIndex.set(index);
    }
  }

  protected submit(): void {
    if (this.basicsForm.invalid) {
      this.currentStepIndex.set(0);
      this.basicsForm.markAllAsTouched();
      return;
    }

    if (isBrandLimitReached(this.tenantService)) {
      promptBrandLimitUpgrade(this.tenantService, this.errorModalService);
      return;
    }

    const { name, tagline, industry, description, location, website } = this.basicsForm.getRawValue();
    const { instagram, businessAge, businessEstablishDate } = this.identityForm.getRawValue();
    const { uniqueValue, admiredBrand1, admiredBrand2, admiredBrand3 } = this.positioningForm.getRawValue();

    // Upload the logo file first (if any) so the brand profile stores a short /media/ path
    // instead of the base64 preview data URL, then create the profile with that path.
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

    const input: CreateBrandProfileInput = {
      name,
      tagline: tagline || undefined,
      industry: industry || undefined,
      description: description || undefined,
      location: location || undefined,
      websiteUrl: website || undefined,
      tones: this.selectedTones(),
      colors: this.brandColors().filter(Boolean),
      supportedLanguages: this.selectedLanguages(),
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
    };

    this.loaderService.show();
    logoUrl$
      .pipe(switchMap(logoUrl => this.brandProfileService.create({ ...input, logoUrl })))
      .subscribe({
        next: res => {
          this.loaderService.hide();
          if (res.status !== 'success' || !res.data) {
            this.errorModalService.show(res.message ?? 'تعذّر إنشاء ملف العلامة التجارية.', { variant: 'error' });
            return;
          }
          this.tenantService.refresh().subscribe();
          this.router.navigate(['/dashboard/brand-profiles']);
        },
        error: err => {
          this.loaderService.hide();
          this.errorModalService.show(
            extractApiErrorMessage(err, 'تعذّر إنشاء ملف العلامة التجارية. يرجى المحاولة مرة أخرى.'),
            { variant: 'error' },
          );
        },
      });
  }
}
