import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { FormErrorsService } from '../../../../services/form-errors.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { TenantService } from '../../../../core/tenant/tenant.service';
import { LoaderService } from '../../../../services/loader.service';
import { applyAuthFailure, applyFieldErrors, hasFieldErrors } from '../../../../core/auth/api-error.util';
import { usernameValidators } from '../../../../core/auth/username.validators';
import { passwordValidators } from '../../../../core/auth/password.validators';

/** Mirrors RegisterCommandValidator's `RuleFor(x => x.FullName).MaximumLength(150)` — the backend
 *  validates the combined `firstName + ' ' + lastName` string sent as one FullName field. */
const FULL_NAME_MAX_LENGTH = 150;

/** Backend `Result.Failure` messages (RegisterCommandHandler) that naturally belong to a specific
 *  field — shown inline under that field instead of a generic banner. */
const REGISTER_FAILURE_FIELD_MAP: Record<string, string> = {
  'A user with this email already exists.': 'email',
  'A user with this username already exists.': 'username',
};

@Component({
  selector: 'app-sign-up-form',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './sign-up-form.html',
  styleUrl: './sign-up-form.css',
})
export class SignUpForm {
  protected readonly form;

  protected readonly submitted = signal(false);
  /** Non-field failure message (e.g. network error) shown as a small inline banner — never a modal. */
  protected readonly formError = signal<string | null>(null);

  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly tenantService = inject(TenantService);
  private readonly loaderService = inject(LoaderService);

  constructor(
    private readonly fb: FormBuilder,
    private readonly formErrorsService: FormErrorsService,
  ) {
    this.form = this.fb.nonNullable.group({
      firstName: ['', [Validators.required]],
      lastName: ['', [Validators.required]],
      username: ['', usernameValidators],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
      password: ['', passwordValidators],
      confirmPassword: ['', [Validators.required]],
      termsAccepted: [false, [Validators.requiredTrue]],
    });
  }

  protected onSubmit(): void {
    this.submitted.set(true);
    this.formError.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { firstName, lastName, username, email, password, confirmPassword } = this.form.getRawValue();
    if (password !== confirmPassword) {
      return;
    }

    const fullName = `${firstName} ${lastName}`.trim();
    if (fullName.length > FULL_NAME_MAX_LENGTH) {
      return;
    }

    this.loaderService.show();
    this.authService.register({ email, username, password, fullName, preferredLanguage: 'Ar' }).subscribe({
      next: res => {
        this.loaderService.hide();
        if (res.status !== 'success' || !res.data) {
          this.formError.set(res.message ?? 'تعذّر إنشاء الحساب. يرجى المحاولة مرة أخرى.');
          return;
        }

        this.tenantService.refreshMemberships().subscribe();
        this.authService.fetchMyProfile().subscribe();
        this.router.navigate(['/dashboard']);
      },
      error: err => {
        this.loaderService.hide();
        applyFieldErrors(this.form, err);
        // Field-level errors already show inline under their control. Anything else (duplicate
        // email/username, network/server errors) is resolved to an inline field message or a
        // small banner by applyAuthFailure — never a popup modal.
        if (!hasFieldErrors(err)) {
          this.formError.set(
            applyAuthFailure(this.form, err, REGISTER_FAILURE_FIELD_MAP, 'تعذّر إنشاء الحساب. يرجى المحاولة مرة أخرى.'),
          );
        }
      },
    });
  }

  protected errorMessage(
    controlName:
      | 'firstName'
      | 'lastName'
      | 'username'
      | 'email'
      | 'confirmPassword'
      | 'termsAccepted',
  ): string | null {
    const control = this.form.controls[controlName];

    const messagesByControl = {
      firstName: { required: 'الاسم الأول مطلوب.' },
      lastName: { required: 'الاسم الأخير مطلوب.' },
      username: {
        required: 'اسم المستخدم مطلوب.',
        minlength: 'اسم المستخدم يجب أن يكون 3 أحرف على الأقل.',
        maxlength: 'اسم المستخدم يجب ألا يتجاوز 30 حرفًا.',
        pattern: 'يجب أن يبدأ اسم المستخدم بحرف، ويحتوي على أحرف وأرقام وشرطة سفلية (_) فقط.',
      },
      email: {
        required: 'البريد الإلكتروني مطلوب.',
        email: 'أدخل بريدًا إلكترونيًا صحيحًا.',
        maxlength: 'البريد الإلكتروني يجب ألا يتجاوز 255 حرفًا.',
      },
      confirmPassword: { required: 'تأكيد كلمة المرور مطلوب.' },
      termsAccepted: { requiredTrue: 'يجب الموافقة على الشروط والأحكام للمتابعة.' },
    } as const;

    return this.formErrorsService.getControlErrorMessage(control, this.submitted(), messagesByControl[controlName]);
  }

  protected passwordsMismatchMessage(): string | null {
    const { password, confirmPassword } = this.form.getRawValue();
    return this.formErrorsService.getPasswordsMismatchMessage(password, confirmPassword, this.submitted());
  }

  /** Cross-field check mirroring the backend's combined-FullName length limit (see
   *  RegisterCommandValidator) — firstName/lastName have no maxlength of their own since the
   *  backend only ever sees them joined into one FullName string. */
  protected fullNameTooLongMessage(): string | null {
    if (!this.submitted() && !this.form.controls.lastName.dirty) return null;
    const { firstName, lastName } = this.form.getRawValue();
    const fullName = `${firstName} ${lastName}`.trim();
    return fullName.length > FULL_NAME_MAX_LENGTH
      ? `الاسم الكامل يجب ألا يتجاوز ${FULL_NAME_MAX_LENGTH} حرفًا.`
      : null;
  }

  // ── Live password requirements checklist (updates on every keystroke) ──

  protected get passwordValue(): string {
    return this.form.controls.password.value ?? '';
  }

  protected passwordTouched(): boolean {
    return this.form.controls.password.dirty || this.submitted();
  }

  protected passwordMeetsMinLength(): boolean {
    return this.passwordValue.length >= 8;
  }

  protected passwordHasUppercase(): boolean {
    return /[A-Z]/.test(this.passwordValue);
  }

  protected passwordHasLowercase(): boolean {
    return /[a-z]/.test(this.passwordValue);
  }

  protected passwordHasDigit(): boolean {
    return /[0-9]/.test(this.passwordValue);
  }

  protected passwordHasSpecialChar(): boolean {
    return /[^a-zA-Z0-9]/.test(this.passwordValue);
  }
}
