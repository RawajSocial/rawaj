import { ChangeDetectionStrategy, Component, effect, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { ModalShell } from '../../../../shared/components/modal-shell/modal-shell';
import { BrandProfilesApiService } from '../../../../core/api/brand-profiles-api.service';
import { ApiError } from '../../../../core/api';
import {
  BrandProfileDetail,
  BrandProfileSummary,
  BrandVoice,
  CreateBrandProfileResponse,
  UpdateBrandProfileResponse,
} from '../../../../core/models';

@Component({
  selector: 'app-brand-profile-form-modal',
  imports: [ReactiveFormsModule, ModalShell],
  templateUrl: './brand-profile-form-modal.html',
  styleUrl: './brand-profile-form-modal.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BrandProfileFormModal {
  private readonly brandProfilesApi = inject(BrandProfilesApiService);

  readonly open = input(false);
  /** When set, the modal edits this brand (its full detail is fetched on open); otherwise it creates a new one. */
  readonly editingBrand = input<BrandProfileSummary | null>(null);

  readonly closed = output<void>();
  readonly saved = output<void>();

  protected readonly isEditMode = () => this.editingBrand() !== null;

  protected readonly brandVoices: { value: BrandVoice; label: string }[] = [
    { value: 'Professional', label: 'احترافي' },
    { value: 'Playful', label: 'مرح' },
    { value: 'Bold', label: 'جريء' },
    { value: 'Friendly', label: 'ودود' },
    { value: 'Formal', label: 'رسمي' },
  ];

  protected readonly loadingDetail = signal(false);
  protected readonly submitting = signal(false);
  protected readonly submitError = signal<string | null>(null);

  private readonly fb = new FormBuilder();
  protected readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    description: [''],
    brandVoice: ['' as BrandVoice | ''],
    tagline: [''],
    industry: [''],
    targetAudience: [''],
    colors: [''],
    logoUrl: [''],
    websiteUrl: [''],
    keywords: [''],
  });

  constructor() {
    effect(() => {
      if (!this.open()) return;
      this.submitError.set(null);

      const editing = this.editingBrand();
      if (!editing) {
        this.form.reset({
          name: '', description: '', brandVoice: '', tagline: '', industry: '',
          targetAudience: '', colors: '', logoUrl: '', websiteUrl: '', keywords: '',
        });
        return;
      }

      this.loadingDetail.set(true);
      this.brandProfilesApi.getById(editing.brandProfileId).subscribe({
        next: (detail) => {
          this.loadingDetail.set(false);
          this.applyDetail(detail);
        },
        error: (error: unknown) => {
          this.loadingDetail.set(false);
          this.submitError.set(error instanceof ApiError ? error.message : 'تعذر تحميل بيانات العلامة التجارية.');
        },
      });
    });
  }

  private applyDetail(detail: BrandProfileDetail): void {
    this.form.reset({
      name: detail.name,
      description: detail.description ?? '',
      brandVoice: detail.brandVoice ?? '',
      tagline: detail.tagline ?? '',
      industry: detail.industry ?? '',
      targetAudience: detail.targetAudience ?? '',
      colors: detail.colors.join(', '),
      logoUrl: detail.logoUrl ?? '',
      websiteUrl: detail.websiteUrl ?? '',
      keywords: detail.keywords.join(', '),
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const payload = {
      name: raw.name,
      description: raw.description || null,
      brandVoice: raw.brandVoice || null,
      tagline: raw.tagline || null,
      industry: raw.industry || null,
      targetAudience: raw.targetAudience || null,
      colors: this.splitList(raw.colors),
      logoUrl: raw.logoUrl || null,
      websiteUrl: raw.websiteUrl || null,
      keywords: this.splitList(raw.keywords),
    };

    this.submitting.set(true);
    this.submitError.set(null);

    const editing = this.editingBrand();
    const request$: Observable<CreateBrandProfileResponse | UpdateBrandProfileResponse> = editing
      ? this.brandProfilesApi.update(editing.brandProfileId, payload)
      : this.brandProfilesApi.create(payload);

    request$.subscribe({
      next: () => {
        this.submitting.set(false);
        this.saved.emit();
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        this.submitError.set(error instanceof ApiError ? error.message : 'تعذر حفظ العلامة التجارية.');
      },
    });
  }

  private splitList(value: string): string[] {
    return value.split(',').map((v) => v.trim()).filter((v) => v.length > 0);
  }
}
