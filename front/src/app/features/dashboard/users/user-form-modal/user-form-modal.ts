import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  effect,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TenantMemberRole } from '../../../../model/tenant.model';
import { BrandProfile } from '../../../../model/brand-profile.model';
import { animateModalIn, animateModalOut } from '../../../../shared/utils/modal-motion';

export interface UserFormValue {
  email: string;
  role: TenantMemberRole;
  brandProfileIds: string[];
  allocatedCoins: number;
}

/** Invite-only modal — an existing member's role/brand-access/coins are edited from the profile
 *  page instead (there's no in-place "edit" here since email can never change once invited). */
@Component({
  selector: 'app-user-form-modal',
  imports: [ReactiveFormsModule],
  templateUrl: './user-form-modal.html',
  styleUrls: ['../users-shared.css', './user-form-modal.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserFormModal {
  readonly open = input.required<boolean>();
  readonly brandProfiles = input<BrandProfile[]>([]);

  readonly closed = output<void>();
  readonly save = output<UserFormValue>();

  private readonly panelRef = viewChild<ElementRef<HTMLElement>>('panel');
  private readonly backdropRef = viewChild<ElementRef<HTMLElement>>('backdrop');
  private readonly closing = signal(false);
  protected readonly submitted = signal(false);
  protected readonly selectedBrandIds = signal<Set<string>>(new Set());

  protected readonly roleOptions: { value: TenantMemberRole; label: string }[] = [
    { value: 'Admin', label: 'مدير' },
    { value: 'Editor', label: 'محرر' },
    { value: 'Viewer', label: 'مشاهد' },
  ];

  private readonly fb = new FormBuilder();
  protected readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    role: ['Editor' as TenantMemberRole, [Validators.required]],
    allocatedCoins: [0, [Validators.required, Validators.min(0)]],
  });

  constructor() {
    effect(() => {
      if (!this.open()) return;
      this.closing.set(false);
      this.submitted.set(false);
      this.selectedBrandIds.set(new Set());
      this.form.reset({ email: '', role: 'Editor', allocatedCoins: 0 });
      queueMicrotask(() => {
        const panel = this.panelRef()?.nativeElement;
        const backdrop = this.backdropRef()?.nativeElement;
        if (panel && backdrop) animateModalIn(panel, backdrop);
      });
    });
  }

  protected toggleBrand(brandProfileId: string): void {
    this.selectedBrandIds.update(current => {
      const next = new Set(current);
      if (next.has(brandProfileId)) next.delete(brandProfileId);
      else next.add(brandProfileId);
      return next;
    });
  }

  protected requestClose(): void {
    if (this.closing()) return;
    const panel = this.panelRef()?.nativeElement;
    const backdrop = this.backdropRef()?.nativeElement;
    if (!panel || !backdrop) {
      this.closed.emit();
      return;
    }
    this.closing.set(true);
    animateModalOut(panel, backdrop).then(() => this.closed.emit());
  }

  protected submit(): void {
    this.submitted.set(true);
    if (this.form.invalid || this.selectedBrandIds().size === 0) {
      this.form.markAllAsTouched();
      return;
    }
    const value = this.form.getRawValue();
    this.save.emit({
      email: value.email,
      role: value.role,
      allocatedCoins: value.allocatedCoins,
      brandProfileIds: Array.from(this.selectedBrandIds()),
    });
  }
}
