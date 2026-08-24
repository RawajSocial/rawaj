import { Component, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormErrorsService } from '../../../../services/form-errors.service';
import { TeamMemberService } from '../../../../services/team-member.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { LoaderService } from '../../../../services/loader.service';
import { usernameValidators } from '../../../../core/auth/username.validators';
import { passwordValidators } from '../../../../core/auth/password.validators';
import { applyFieldErrors, extractApiErrorMessage, hasFieldErrors } from '../../../../core/auth/api-error.util';

/** Registration form for a brand-new invitee (no Rawaj account yet). Submitting registers the
 *  account, provisions their own (unactivated) tenant, and joins the inviting tenant — all in one
 *  call — then lands them logged in. */
@Component({
  selector: 'app-invite-form',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './invite-form.html',
  styleUrl: './invite-form.css',
})
export class InviteForm {
  readonly token = input.required<string>();
  readonly email = input.required<string>();

  readonly registered = output<void>();

  protected readonly form;
  protected readonly submitted = signal(false);
  protected readonly formError = signal<string | null>(null);

  private readonly teamMemberService = inject(TeamMemberService);
  private readonly authService = inject(AuthService);
  private readonly loaderService = inject(LoaderService);

  constructor(
    private readonly fb: FormBuilder,
    private readonly formErrorsService: FormErrorsService,
  ) {
    this.form = this.fb.nonNullable.group({
      fullName: ['', [Validators.required, Validators.minLength(2)]],
      username: ['', usernameValidators],
      password: ['', passwordValidators],
    });
  }

  protected onSubmit(): void {
    this.submitted.set(true);
    this.formError.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { fullName, username, password } = this.form.getRawValue();

    this.loaderService.show();
    this.teamMemberService
      .acceptInvitationAndRegister(this.token(), { username, password, fullName, preferredLanguage: 'Ar' })
      .subscribe({
        next: res => {
          this.loaderService.hide();
          if (res.status !== 'success' || !res.data) {
            this.formError.set(res.message ?? 'تعذّر إنشاء الحساب. يرجى المحاولة مرة أخرى.');
            return;
          }
          this.authService.applyExternalSession(res.data.accessToken, res.data.refreshToken);
          this.registered.emit();
        },
        error: err => {
          this.loaderService.hide();
          applyFieldErrors(this.form, err);
          if (!hasFieldErrors(err)) {
            this.formError.set(extractApiErrorMessage(err, 'تعذّر إنشاء الحساب. يرجى المحاولة مرة أخرى.'));
          }
        },
      });
  }

  protected errorMessage(controlName: 'fullName' | 'username' | 'password'): string | null {
    if (controlName === 'fullName') {
      return this.formErrorsService.getControlErrorMessage(this.form.controls.fullName, this.submitted(), {
        required: 'الاسم الكامل مطلوب.',
        minlength: 'الاسم يجب أن يكون حرفين على الأقل.',
      });
    }
    if (controlName === 'username') {
      return this.formErrorsService.getControlErrorMessage(this.form.controls.username, this.submitted(), {
        required: 'اسم المستخدم مطلوب.',
        minlength: 'اسم المستخدم يجب أن يكون 3 أحرف على الأقل.',
        maxlength: 'اسم المستخدم طويل جدًا.',
        pattern: 'يجب أن يبدأ اسم المستخدم بحرف، ويحتوي على أحرف وأرقام وشرطة سفلية (_) فقط.',
      });
    }
    return this.formErrorsService.getControlErrorMessage(this.form.controls.password, this.submitted(), {
      required: 'كلمة المرور مطلوبة.',
      minlength: 'كلمة المرور يجب أن تكون 8 أحرف على الأقل.',
      hasUppercase: 'يجب أن تحتوي على حرف كبير واحد على الأقل.',
      hasLowercase: 'يجب أن تحتوي على حرف صغير واحد على الأقل.',
      hasDigit: 'يجب أن تحتوي على رقم واحد على الأقل.',
      hasSpecialChar: 'يجب أن تحتوي على رمز خاص واحد على الأقل.',
    });
  }
}
