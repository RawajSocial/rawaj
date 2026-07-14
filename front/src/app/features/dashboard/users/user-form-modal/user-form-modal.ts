import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  effect,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TeamMember, TeamMemberRole } from '../../../../model/team-member.model';
import { animateModalIn, animateModalOut } from '../../../../shared/utils/modal-motion';

export interface UserFormValue {
  name: string;
  email: string;
  role: TeamMemberRole;
  department: string;
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
  readonly member = input<TeamMember | null>(null);

  readonly closed = output<void>();
  readonly save = output<UserFormValue>();

  protected readonly mode = computed<'invite' | 'edit'>(() => (this.member() ? 'edit' : 'invite'));

  private readonly panelRef = viewChild<ElementRef<HTMLElement>>('panel');
  private readonly backdropRef = viewChild<ElementRef<HTMLElement>>('backdrop');
  private readonly closing = signal(false);

  protected readonly roleOptions: { value: TeamMemberRole; label: string }[] = [
    { value: 'admin', label: 'مدير' },
    { value: 'editor', label: 'محرر' },
    { value: 'moderator', label: 'مشرف' },
    { value: 'viewer', label: 'مشاهد' },
  ];

  private readonly fb = new FormBuilder();
  protected readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    role: ['editor' as TeamMemberRole, [Validators.required]],
    department: ['', [Validators.required]],
  });

  constructor() {
    effect(() => {
      const m = this.member();
      this.form.reset({
        name: m?.name ?? '',
        email: m?.email ?? '',
        role: m?.role ?? 'editor',
        department: m?.department ?? '',
      });
    });

    effect(() => {
      if (!this.open()) return;
      this.closing.set(false);
      queueMicrotask(() => {
        const panel = this.panelRef()?.nativeElement;
        const backdrop = this.backdropRef()?.nativeElement;
        if (panel && backdrop) animateModalIn(panel, backdrop);
      });
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
    this.save.emit(this.form.getRawValue());
  }
}
