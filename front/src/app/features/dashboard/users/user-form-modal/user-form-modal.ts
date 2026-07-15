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
import { TeamMemberRole } from '../../../../model/team-member.model';
import { animateModalIn, animateModalOut } from '../../../../shared/utils/modal-motion';

export interface UserFormValue {
  email: string;
  role: TeamMemberRole;
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

  readonly closed = output<void>();
  readonly save = output<UserFormValue>();

  private readonly panelRef = viewChild<ElementRef<HTMLElement>>('panel');
  private readonly backdropRef = viewChild<ElementRef<HTMLElement>>('backdrop');
  private readonly closing = signal(false);

  protected readonly roleOptions: { value: TeamMemberRole; label: string }[] = [
    { value: 'Admin', label: 'مدير' },
    { value: 'Editor', label: 'محرر' },
    { value: 'Viewer', label: 'مشاهد' },
  ];

  private readonly fb = new FormBuilder();
  protected readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    role: ['Editor' as TeamMemberRole, [Validators.required]],
  });

  constructor() {
    effect(() => {
      if (!this.open()) return;
      this.form.reset({ email: '', role: 'Editor' });
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
