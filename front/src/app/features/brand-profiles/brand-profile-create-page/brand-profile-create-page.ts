import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { FileUpload } from '../../../shared/components/file-upload/file-upload';
import { BrandProfileService } from '../../../services/brand-profile.service';
import { BrandVoice } from '../../../model/brand-profile.model';
import { SeoService } from '../../../services/seo.service';
import { TenantService } from '../../../core/tenant/tenant.service';
import { ErrorModalService } from '../../../services/error-modal.service';
import { LoaderService } from '../../../services/loader.service';
import { extractApiErrorMessage } from '../../../core/auth/api-error.util';

interface WizardStep {
  key: 'basics' | 'voice' | 'logo' | 'review';
  label: string;
}

const STEPS: WizardStep[] = [
  { key: 'basics', label: 'الأساسيات' },
  { key: 'voice', label: 'أسلوب العلامة' },
  { key: 'logo', label: 'الشعار' },
  { key: 'review', label: 'المراجعة' },
];

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

  protected readonly progressPercent = () =>
    Math.round(((this.currentStepIndex() + 1) / this.steps.length) * 100);

  protected readonly voiceOptions: { value: BrandVoice; label: string; desc: string }[] = [
    { value: 'professional', label: 'احترافي', desc: 'نبرة رسمية وموثوقة' },
    { value: 'friendly', label: 'ودود', desc: 'نبرة قريبة ومرحّبة' },
    { value: 'bold', label: 'جريء', desc: 'نبرة واثقة ومباشرة' },
    { value: 'playful', label: 'مرح', desc: 'نبرة خفيفة ومرحة' },
    { value: 'luxurious', label: 'فاخر', desc: 'نبرة راقية وحصرية' },
  ];

  private readonly fb = new FormBuilder();
  protected readonly basicsForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    tagline: [''],
    industry: [''],
    description: [''],
    location: [''],
  });

  protected readonly selectedVoice = signal<BrandVoice>('professional');
  protected readonly logoDataUrl = signal<string | null>(null);
  protected readonly logoFileName = signal<string | null>(null);

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

  protected selectVoice(voice: BrandVoice): void {
    this.selectedVoice.set(voice);
  }

  protected onLogoSelected(file: File): void {
    this.logoFileName.set(file.name);
    const reader = new FileReader();
    reader.onload = e => this.logoDataUrl.set(e.target?.result as string);
    reader.readAsDataURL(file);
  }

  protected onLogoCleared(): void {
    this.logoDataUrl.set(null);
    this.logoFileName.set(null);
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

    if (this.tenantService.brandProfileCount() >= this.tenantService.maxBrands() && !this.tenantService.isAgency()) {
      this.errorModalService.show(
        'حسابك الحالي كصاحب علامة تجارية يسمح بملف تعريف واحد فقط. رقِّ حسابك إلى وكالة تسويق لإدارة أكثر من علامة.',
        { variant: 'warning', title: 'يلزم ترقية الحساب' },
      );
      this.router.navigate(['/upgrade-tenant']);
      return;
    }

    const { name, tagline, industry, description, location } = this.basicsForm.getRawValue();

    this.loaderService.show();
    this.brandProfileService
      .create({
        name,
        tagline: tagline || undefined,
        industry: industry || undefined,
        description: description || undefined,
        location: location || undefined,
        brandVoice: this.selectedVoice(),
        logoUrl: this.logoDataUrl() ?? undefined,
      })
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
