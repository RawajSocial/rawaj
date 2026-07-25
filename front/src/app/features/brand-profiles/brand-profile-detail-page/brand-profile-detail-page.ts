import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { of, switchMap, throwError } from 'rxjs';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { FileUpload } from '../../../shared/components/file-upload/file-upload';
import { SelectDropdown, SelectOption } from '../../../shared/components/select-dropdown/select-dropdown';
import { BrandProfileService } from '../../../services/brand-profile.service';
import { TenantService } from '../../../core/tenant/tenant.service';
import { BrandProfileDetail, BrandVoice, BRAND_PROFILE_STATUS_LABELS, BRAND_VOICE_LABELS } from '../../../model/brand-profile.model';
import { SeoService } from '../../../services/seo.service';
import { ErrorModalService } from '../../../services/error-modal.service';
import { LoaderService } from '../../../services/loader.service';
import { extractApiErrorMessage } from '../../../core/auth/api-error.util';

const VOICE_OPTIONS: SelectOption[] = [
  { value: 'professional', label: 'احترافي' },
  { value: 'friendly', label: 'ودود' },
  { value: 'bold', label: 'جريء' },
  { value: 'playful', label: 'مرح' },
  { value: 'luxurious', label: 'فاخر' },
];

/** Turns a comma-separated field back into a clean string[] for the update request; `undefined`
 *  (rather than `[]`) when the field was left blank so the partial-update backend leaves the
 *  existing value untouched instead of wiping it. */
function parseList(raw: string): string[] | undefined {
  const items = raw.split(',').map(s => s.trim()).filter(Boolean);
  return items.length > 0 ? items : undefined;
}

@Component({
  selector: 'app-brand-profile-detail-page',
  imports: [RouterLink, ReactiveFormsModule, PageHeader, FileUpload, SelectDropdown],
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
  protected readonly voiceLabels = BRAND_VOICE_LABELS;
  protected readonly voiceOptions = VOICE_OPTIONS;

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
    brandVoice: ['professional' as BrandVoice],
    colors: [''],
    supportedLanguages: [''],
    keywords: [''],
  });

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
          brandVoice: res.data.brandVoice ?? 'professional',
          colors: res.data.colors.join(', '),
          supportedLanguages: res.data.supportedLanguages.join(', '),
          keywords: res.data.keywords.join(', '),
        });
        this.logoDataUrl.set(res.data.logoUrl ?? null);
      },
      error: () => {
        this.loading.set(false);
        this.notFound.set(true);
      },
    });
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

    const { name, tagline, industry, targetAudience, description, websiteUrl, brandVoice, colors, supportedLanguages, keywords } =
      this.form.getRawValue();

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
            brandVoice,
            colors: parseList(colors),
            supportedLanguages: parseList(supportedLanguages),
            keywords: parseList(keywords),
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
