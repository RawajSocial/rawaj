import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TeamMember, TeamMemberRole } from '../../../../model/team-member.model';
import { animateModalIn, animateModalOut } from '../../../../shared/utils/modal-motion';
import { TenantService } from '../../../../core/tenant/tenant.service';

export interface UserFormValue {
  email: string;
  role: TeamMemberRole;
  brandProfileIds: string[];
}

export interface UserUpdateValue {
  role: TeamMemberRole;
  brandProfileIds: string[];
}

@Component({
  selector: 'app-user-form-modal',
  imports: [ReactiveFormsModule],
  templateUrl: './user-form-modal.html',
  styleUrls: ['../users-shared.css', './user-form-modal.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserFormModal {
  readonly open = input.required<boolean>();
  readonly submitting = input(false);
  readonly serverError = input<string | null>(null);
  /** When set, the modal renders in edit mode (no email field, pre-filled role/brands) and emits
   * `update` instead of `save`. */
  readonly editingMember = input<TeamMember | null>(null);

  readonly closed = output<void>();
  readonly save = output<UserFormValue>();
  readonly update = output<UserUpdateValue>();

  protected readonly isEditMode = computed(() => this.editingMember() !== null);

  private readonly panelRef = viewChild<ElementRef<HTMLElement>>('panel');
  private readonly backdropRef = viewChild<ElementRef<HTMLElement>>('backdrop');
  private readonly closing = signal(false);
  private readonly tenantService = inject(TenantService);

  protected readonly roleOptions: { value: TeamMemberRole; label: string }[] = [
    { value: 'Admin', label: 'مدير' },
    { value: 'Editor', label: 'محرر' },
    { value: 'Viewer', label: 'مشاهد' },
  ];

  protected readonly brandProfiles = this.tenantService.brandProfiles;
  protected readonly selectedRole = signal<TeamMemberRole>('Editor');
  protected readonly selectedBrandIds = signal<Set<string>>(new Set());

  /** Owner/Admin manage every brand in the tenant by default - only Editor/Viewer need an explicit
   * brand selection, matching the backend's brand-scoping rule. */
  protected readonly needsBrandSelection = computed(() => this.selectedRole() !== 'Admin');

  private readonly fb = new FormBuilder();
  protected readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    role: ['Editor' as TeamMemberRole, [Validators.required]],
  });

  constructor() {
    effect(() => {
      if (!this.open()) return;
      const editing = this.editingMember();

      if (editing) {
        this.form.controls.email.clearValidators();
        this.form.controls.email.updateValueAndValidity();
        this.form.reset({ email: editing.email, role: editing.role as TeamMemberRole });
        this.selectedRole.set(editing.role as TeamMemberRole);
        this.selectedBrandIds.set(new Set(editing.brandProfileIds));
      } else {
        this.form.controls.email.setValidators([Validators.required, Validators.email]);
        this.form.controls.email.updateValueAndValidity();
        this.form.reset({ email: '', role: 'Editor' });
        this.selectedRole.set('Editor');
        this.selectedBrandIds.set(new Set());
      }

      this.closing.set(false);
      queueMicrotask(() => {
        const panel = this.panelRef()?.nativeElement;
        const backdrop = this.backdropRef()?.nativeElement;
        if (panel && backdrop) animateModalIn(panel, backdrop);
      });
    });
  }

  protected onRoleChange(role: string): void {
    this.selectedRole.set(role as TeamMemberRole);
  }

  protected toggleBrand(brandProfileId: string): void {
    this.selectedBrandIds.update((current) => {
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
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    if (this.needsBrandSelection() && this.selectedBrandIds().size === 0) {
      return;
    }
    const { email, role } = this.form.getRawValue();
    const brandProfileIds = Array.from(this.selectedBrandIds());

    if (this.isEditMode()) {
      this.update.emit({ role, brandProfileIds });
    } else {
      this.save.emit({ email, role, brandProfileIds });
    }
  }
}
