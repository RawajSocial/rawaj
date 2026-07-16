import { Component, effect, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormErrorsService } from '../../../../services/form-errors.service';
import { TeamMember } from '../../../../model/team-member.model';

@Component({
  selector: 'app-invite-form',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './invite-form.html',
  styleUrl: './invite-form.css',
})
export class InviteForm {
  readonly invitedMember = input<TeamMember | undefined>();

  protected readonly form;
  protected submitted = false;

  constructor(
    private readonly fb: FormBuilder,
    private readonly formErrorsService: FormErrorsService,
  ) {
    this.form = this.fb.nonNullable.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
    });

    // The invitee's email is fixed by the invite itself — prefill and lock
    // it once we know who was invited.
    effect(() => {
      const member = this.invitedMember();
      if (member) {
        this.form.controls.email.setValue(member.email);
        this.form.controls.email.disable();
      }
    });
  }

  protected onSubmit(): void {
    this.submitted = true;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    console.log('Invite login payload', this.form.getRawValue());
  }

  protected errorMessage(controlName: 'email' | 'password'): string | null {
    if (controlName === 'email') {
      return this.formErrorsService.getControlErrorMessage(this.form.controls.email, this.submitted, {
        required: 'البريد الإلكتروني مطلوب.',
        email: 'أدخل بريدًا إلكترونيًا صحيحًا.',
      });
    }

    return this.formErrorsService.getControlErrorMessage(this.form.controls.password, this.submitted, {
      required: 'كلمة المرور مطلوبة.',
      minlength: 'كلمة المرور يجب أن تكون 8 أحرف على الأقل.',
    });
  }
}
