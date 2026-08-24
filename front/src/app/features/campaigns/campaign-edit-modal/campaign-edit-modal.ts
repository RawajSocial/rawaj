import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CampaignService } from '../../../services/campaign.service';
import { ErrorModalService } from '../../../services/error-modal.service';
import { FormErrorsService } from '../../../services/form-errors.service';
import { PermissionService } from '../../../core/tenant/permission.service';
import { ModalShell } from '../../../shared/components/modal-shell/modal-shell';
import { applyFieldErrors, extractApiErrorMessage, hasFieldErrors } from '../../../core/auth/api-error.util';
import {
  BACKEND_TO_CAMPAIGN_PLATFORM, CAMPAIGN_OBJECTIVE_LABELS, CAMPAIGN_PLATFORM_META,
  CampaignObjective, CampaignPlatform, GetCampaignResponse, UpdateCampaignInput,
} from '../../../model/campaign.model';
import { BackendSocialPlatform } from '../../../model/content-item.model';

/** The frontend platform slug → the backend `SocialPlatform` enum name the API expects. The
 *  inverse of BACKEND_TO_CAMPAIGN_PLATFORM — only Facebook and Instagram exist there, so those
 *  are the only platforms a campaign can ever be saved targeting. */
const CAMPAIGN_TO_BACKEND_PLATFORM: Record<string, BackendSocialPlatform> = Object.fromEntries(
  Object.entries(BACKEND_TO_CAMPAIGN_PLATFORM).map(([backend, front]) => [front, backend as BackendSocialPlatform]),
);

const EDITABLE_PLATFORMS = Object.keys(CAMPAIGN_TO_BACKEND_PLATFORM) as CampaignPlatform[];

const OBJECTIVE_OPTIONS = (Object.keys(CAMPAIGN_OBJECTIVE_LABELS) as CampaignObjective[])
  .map(value => ({ value, label: CAMPAIGN_OBJECTIVE_LABELS[value] }));

/**
 * Edits a campaign's own details. `PUT /campaigns/{id}` has always existed and been fully wired,
 * but nothing in the UI called it outside the onboarding wizard's silent autosave — so a name,
 * budget, date or platform set once during onboarding could never be corrected afterwards.
 *
 * Sends only the fields the user actually changed: the endpoint is a patch (every field is
 * nullable and skipped when null), and the wizard's autosave uses the same command, so blindly
 * sending the whole form would fight it and rewrite untouched columns on every save.
 */
@Component({
  selector: 'app-campaign-edit-modal',
  imports: [ModalShell, ReactiveFormsModule],
  templateUrl: './campaign-edit-modal.html',
  styleUrl: './campaign-edit-modal.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CampaignEditModal {
  readonly open = input.required<boolean>();
  readonly campaign = input.required<GetCampaignResponse | null>();
  /** Emits the updated record so the host page can refresh without a second GET. */
  readonly saved = output<GetCampaignResponse>();
  readonly closed = output<void>();

  private readonly fb = inject(FormBuilder);
  private readonly campaignService = inject(CampaignService);
  private readonly errorModalService = inject(ErrorModalService);
  private readonly formErrors = inject(FormErrorsService);
  protected readonly perms = inject(PermissionService);

  protected readonly platformOptions = EDITABLE_PLATFORMS;
  protected readonly platformMeta = CAMPAIGN_PLATFORM_META;
  protected readonly objectiveOptions = OBJECTIVE_OPTIONS;

  protected readonly saving = signal(false);
  protected readonly submitted = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly selectedPlatforms = signal<Set<CampaignPlatform>>(new Set());

  protected readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(255)]],
    objective: [''],
    startDate: [''],
    endDate: [''],
    budgetAmount: [null as number | null, [Validators.min(0)]],
    budgetCurrency: [''],
  });

  /** The form's value as a signal, so the cross-field check below re-runs as the user types. */
  private readonly formValue = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });

  /** Mirrors the backend's cross-field rule (UpdateCampaignCommandValidator, plus the handler's
   *  check against the stored value) so an impossible range is caught before a round-trip. Both
   *  dates are `yyyy-MM-dd`, which compares correctly as a string. */
  protected readonly datesOutOfOrder = computed(() => {
    const { startDate, endDate } = this.formValue();
    if (!startDate || !endDate) return false;
    return endDate < startDate;
  });

  protected readonly canSave = computed(() =>
    !this.saving() && this.selectedPlatforms().size > 0 && !this.datesOutOfOrder(),
  );

  constructor() {
    // Re-seeds every time the modal is opened (or the campaign behind it changes), so reopening
    // after a cancel shows the saved values rather than the user's abandoned edits.
    effect(() => {
      const campaign = this.campaign();
      if (!this.open() || !campaign) return;
      this.resetTo(campaign);
    });
  }

  private resetTo(campaign: GetCampaignResponse): void {
    this.submitted.set(false);
    this.formError.set(null);
    this.form.reset({
      name: campaign.name,
      objective: campaign.objective ?? '',
      // The API returns DateOnly as `yyyy-MM-dd`, which is exactly what <input type="date"> wants.
      startDate: campaign.startDate ?? '',
      endDate: campaign.endDate ?? '',
      budgetAmount: campaign.budgetAmount ?? null,
      budgetCurrency: campaign.budgetCurrency ?? '',
    });
    this.selectedPlatforms.set(new Set(
      (campaign.targetPlatforms ?? [])
        .map(p => BACKEND_TO_CAMPAIGN_PLATFORM[p])
        .filter((p): p is CampaignPlatform => !!p),
    ));
  }

  protected isPlatformSelected(p: CampaignPlatform): boolean {
    return this.selectedPlatforms().has(p);
  }

  protected togglePlatform(p: CampaignPlatform): void {
    this.selectedPlatforms.update(set => {
      const next = new Set(set);
      if (next.has(p)) next.delete(p);
      else next.add(p);
      return next;
    });
  }

  protected errorFor(control: 'name' | 'budgetAmount'): string | null {
    return this.formErrors.getControlErrorMessage(this.form.get(control), this.submitted(), {
      required: 'اسم الحملة مطلوب.',
      maxlength: 'يجب ألا يتجاوز اسم الحملة 255 حرفًا.',
    });
  }

  protected save(): void {
    if (!this.perms.canEdit()) return;
    this.submitted.set(true);
    this.formError.set(null);

    const campaign = this.campaign();
    if (!campaign || this.form.invalid || !this.canSave()) return;

    const input = this.buildChangedFields(campaign);
    if (Object.keys(input).length === 0) {
      this.closed.emit();
      return;
    }

    this.saving.set(true);
    this.campaignService.update(campaign.campaignId, input).subscribe({
      next: res => {
        this.saving.set(false);
        if (res.data) this.saved.emit(res.data);
        this.closed.emit();
      },
      error: err => {
        this.saving.set(false);
        // Field-level messages land under their own inputs; only a failure with nothing to attach
        // to gets the banner, so the user never sees the same problem reported twice.
        applyFieldErrors(this.form, err);
        if (!hasFieldErrors(err)) {
          this.formError.set(extractApiErrorMessage(err, 'تعذّر حفظ تعديلات الحملة.'));
        }
      },
    });
  }

  /** Only what actually changed — see the class doc comment on why this is a patch. */
  private buildChangedFields(campaign: GetCampaignResponse): UpdateCampaignInput {
    const v = this.form.getRawValue();
    const input: UpdateCampaignInput = {};

    if (v.name.trim() !== campaign.name) input.name = v.name.trim();
    if ((v.objective || '') !== (campaign.objective ?? '')) input.objective = v.objective;
    if ((v.startDate || '') !== (campaign.startDate ?? '')) input.startDate = v.startDate || undefined;
    if ((v.endDate || '') !== (campaign.endDate ?? '')) input.endDate = v.endDate || undefined;
    if ((v.budgetCurrency || '') !== (campaign.budgetCurrency ?? '')) input.budgetCurrency = v.budgetCurrency;

    const budget = v.budgetAmount === null ? null : Number(v.budgetAmount);
    if (budget !== (campaign.budgetAmount ?? null) && budget !== null) input.budgetAmount = budget;

    const platforms = [...this.selectedPlatforms()].map(p => CAMPAIGN_TO_BACKEND_PLATFORM[p]);
    const current = [...(campaign.targetPlatforms ?? [])].sort().join(',');
    if ([...platforms].sort().join(',') !== current) input.targetPlatforms = platforms;

    return input;
  }

  protected onClose(): void {
    if (this.saving()) return;
    this.closed.emit();
  }
}
