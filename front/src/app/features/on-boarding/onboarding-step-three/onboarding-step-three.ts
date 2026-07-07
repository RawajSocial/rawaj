import { Component, computed, input, output, signal } from '@angular/core';
import { OnboardingStepHeader } from '../onboarding-step-header/onboarding-step-header';
import { OnboardingStepActions } from '../onboarding-step-actions/onboarding-step-actions';
import { StepBadge } from '../../../shared/components/step-badge/step-badge';
import { StepHeading } from '../../../shared/components/step-heading/step-heading';
import { RevealDirective } from '../../../shared/directives/reveal.directive';

@Component({
  selector: 'app-onboarding-step-three',
  imports: [RevealDirective, OnboardingStepHeader, OnboardingStepActions, StepBadge, StepHeading],
  templateUrl: './onboarding-step-three.html',
  styleUrl: './onboarding-step-three.css',
})
export class OnboardingStepThree {
  readonly currentStep = input(3);
  readonly totalSteps = input(7);
  readonly data = input<OnboardingData | null>(null);
  readonly next = output<void>();
  readonly back = output<void>();
  readonly dataChange = output<Partial<OnboardingData>>();

  protected readonly businessAgeOptions = [
    'أقل من 6 أشهر',
    '6 - 12 شهرًا',
    '1 - 3 سنوات',
    '3 - 5 سنوات',
    'أكثر من 5 سنوات',
  ];

  protected readonly stages: StageOption[] = [
    { value: 'إطلاق جديد',  label: 'إطلاق جديد',  icon: 'fa-solid fa-plus',          className: 'stage-new' },
    { value: 'نمو وتسويق',  label: 'نمو وتسويق',  icon: 'fa-solid fa-arrow-trend-up', className: 'stage-trend' },
    { value: 'توسع وتطوير', label: 'توسع وتطوير', icon: 'fa-solid fa-rocket',         className: 'stage-scale' },
    { value: 'إعادة تموضع',label: 'إعادة تموضع',icon: 'fa-solid fa-rotate',         className: 'stage-renew' },
  ];

  protected readonly toneOptions = [
    'احترافي', 'ودود', 'جريء', 'مرح', 'أنيق',
    'ملهم', 'تثقيفي', 'مبتكر', 'محفز', 'ذكي ومضحك',
  ];

  protected readonly languageOptions = [
    { value: 'ar',    label: 'العربية',    icon: 'fa-solid fa-globe',          className: 'lang-ar' },
    { value: 'en',    label: 'الإنجليزية', icon: 'fa-solid fa-language',       className: 'lang-en' },
    { value: 'ar-en', label: 'كلاهما',     icon: 'fa-solid fa-earth-americas', className: 'lang-ar-en' },
  ];

  protected readonly storeOptions: StoreOption[] = [
    { value: 'online',   label: 'إلكتروني فقط',  icon: 'fa-solid fa-globe', className: 'store-online' },
    { value: 'physical', label: 'متجر فعلي',       icon: 'fa-solid fa-store', className: 'store-physical' },
    { value: 'both',     label: 'إلكتروني وفعلي', icon: 'fa-solid fa-shop',  className: 'store-both' },
  ];

  protected readonly priceOptions = [
    { value: 'budget',  label: 'اقتصادي', icon: 'fa-solid fa-coins',          className: 'price-budget' },
    { value: 'mid',     label: 'متوسط',   icon: 'fa-solid fa-scale-balanced', className: 'price-mid' },
    { value: 'premium', label: 'راقٍ',    icon: 'fa-solid fa-gem',            className: 'price-premium' },
    { value: 'luxury',  label: 'فاخر',    icon: 'fa-solid fa-crown',          className: 'price-luxury' },
  ];

  protected readonly platformOptions = [
    { value: 'instagram', label: 'Instagram',   icon: 'fa-brands fa-instagram',  className: 'platform-instagram' },
    { value: 'facebook',  label: 'Facebook',    icon: 'fa-brands fa-facebook',   className: 'platform-facebook' },
    { value: 'tiktok',    label: 'TikTok',      icon: 'fa-brands fa-tiktok',     className: 'platform-tiktok' },
    { value: 'youtube',   label: 'YouTube',     icon: 'fa-brands fa-youtube',    className: 'platform-youtube' },
    { value: 'snapchat',  label: 'Snapchat',    icon: 'fa-brands fa-snapchat',   className: 'platform-snapchat' },
    { value: 'twitter',   label: 'X / Twitter', icon: 'fa-brands fa-x-twitter',  className: 'platform-twitter' },
    { value: 'linkedin',  label: 'LinkedIn',    icon: 'fa-brands fa-linkedin',   className: 'platform-linkedin' },
    { value: 'whatsapp',  label: 'WhatsApp',    icon: 'fa-brands fa-whatsapp',   className: 'platform-whatsapp' },
  ];

  protected readonly selectedTone = computed(() => this.data()?.brandTone ?? []);
  protected readonly selectedLanguages = computed(() => this.data()?.languages ?? []);
  protected readonly selectedPlatforms = computed(() => this.data()?.existingPlatforms ?? []);
  protected readonly brandColors = computed(() => this.data()?.brandColors ?? ['#7c3aed', '', '']);
  protected readonly logoPreviewUrl = signal<string | null>(null);

  protected toggleTone(tone: string): void {
    const current = [...this.selectedTone()];
    const idx = current.indexOf(tone);
    if (idx >= 0) {
      current.splice(idx, 1);
    } else if (current.length < 5) {
      current.push(tone);
    }
    this.emit({ brandTone: current });
  }

  protected toggleLanguage(lang: string): void {
    const current = [...this.selectedLanguages()];
    const idx = current.indexOf(lang);
    if (idx >= 0) {
      current.splice(idx, 1);
    } else {
      current.push(lang);
    }
    this.emit({ languages: current });
  }

  protected togglePlatform(platform: string): void {
    const current = [...this.selectedPlatforms()];
    const idx = current.indexOf(platform);
    if (idx >= 0) {
      current.splice(idx, 1);
    } else {
      current.push(platform);
    }
    this.emit({ existingPlatforms: current });
  }

  protected setColor(index: number, value: string): void {
    const colors = [...this.brandColors()];
    colors[index] = value;
    this.emit({ brandColors: colors });
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

  protected onLogoChange(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.emit({ logoFile: file.name });
    const reader = new FileReader();
    reader.onload = (e) => this.logoPreviewUrl.set(e.target?.result as string);
    reader.readAsDataURL(file);
  }

  protected removeLogo(): void {
    this.logoPreviewUrl.set(null);
    this.emit({ logoFile: undefined });
  }

  protected onGuidelinesChange(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) this.emit({ guidelinesFile: file.name });
  }

  protected emit(partial: Partial<OnboardingData>): void {
    this.dataChange.emit(partial);
  }

  protected onNext(): void { this.next.emit(); }
  protected onBack(): void { this.back.emit(); }
}

type StageOption = { value: string; label: string; icon: string; className: string };
type StoreOption = { value: string; label: string; icon: string; className: string };

type OnboardingData = {
  brandName?: string;
  tagline?: string;
  instagram?: string;
  website?: string;
  sector?: string;
  location?: string;
  businessAge?: string;
  stage?: string;
  brandWord1?: string;
  brandWord2?: string;
  brandWord3?: string;
  brandTone?: string[];
  hasGuidelines?: string;
  guidelinesFile?: string;
  brandColors?: string[];
  logoFile?: string;
  languages?: string[];
  productDesc?: string;
  uniqueValue?: string;
  pricePositioning?: string;
  storePresence?: string;
  existingPlatforms?: string[];
};
