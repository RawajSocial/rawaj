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
import { TeamMember } from '../../../../model/team-member.model';
import { animateModalIn, animateModalOut } from '../../../../shared/utils/modal-motion';

@Component({
  selector: 'app-reset-password-modal',
  imports: [ReactiveFormsModule],
  templateUrl: './reset-password-modal.html',
  styleUrls: ['../users-shared.css', './reset-password-modal.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResetPasswordModal {
  readonly open = input.required<boolean>();
  readonly member = input<TeamMember | null>(null);

  readonly closed = output<void>();
  readonly resetConfirmed = output<string>();

  protected readonly showPassword = signal(false);

  private readonly panelRef = viewChild<ElementRef<HTMLElement>>('panel');
  private readonly backdropRef = viewChild<ElementRef<HTMLElement>>('backdrop');
  private readonly closing = signal(false);

  private readonly fb = new FormBuilder();
  protected readonly form = this.fb.nonNullable.group({
    password: ['', [Validators.required, Validators.minLength(8)]],
    confirm: ['', [Validators.required]],
  });

  constructor() {
    effect(() => {
      if (!this.open()) return;
      this.closing.set(false);
      this.showPassword.set(false);
      this.form.reset({ password: '', confirm: '' });
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

  protected generatePassword(): void {
    const chars = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%';
    let value = '';
    for (let i = 0; i < 12; i++) value += chars[Math.floor(Math.random() * chars.length)];
    this.form.patchValue({ password: value, confirm: value });
    this.showPassword.set(true);
  }

  protected get passwordsMismatch(): boolean {
    const { password, confirm } = this.form.getRawValue();
    return !!confirm && password !== confirm;
  }

  protected submit(): void {
    if (this.form.invalid || this.passwordsMismatch) {
      this.form.markAllAsTouched();
      return;
    }
    this.resetConfirmed.emit(this.form.getRawValue().password);
  }
}
