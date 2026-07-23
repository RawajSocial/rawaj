import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { FormErrorsService } from '../../../../services/form-errors.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { TenantService } from '../../../../core/tenant/tenant.service';
import { ErrorModalService } from '../../../../services/error-modal.service';
import { LoaderService } from '../../../../services/loader.service';
import { extractApiErrorMessage, applyFieldErrors } from '../../../../core/auth/api-error.util';
import { usernameValidators } from '../../../../core/auth/username.validators';

@Component({
  selector: 'app-sign-up-form',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './sign-up-form.html',
  styleUrl: './sign-up-form.css',
})
export class SignUpForm {
  protected readonly form;

  protected readonly submitted = signal(false);

  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly tenantService = inject(TenantService);
  private readonly errorModalService = inject(ErrorModalService);
  private readonly loaderService = inject(LoaderService);

  constructor(
    private readonly fb: FormBuilder,
    private readonly formErrorsService: FormErrorsService,
  ) {
    this.form = this.fb.nonNullable.group({
      firstName: ['', [Validators.required]],
      lastName: ['', [Validators.required]],
      username: ['', usernameValidators],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required, Validators.minLength(8)]],
      termsAccepted: [false, [Validators.requiredTrue]],
    });
  }

  protected onSubmit(): void {
    this.submitted.set(true);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { firstName, lastName, username, email, password, confirmPassword } = this.form.getRawValue();
    if (password !== confirmPassword) {
      return;
    }

    const fullName = `${firstName} ${lastName}`.trim();

    this.loaderService.show();
    this.authService.register({ email, username, password, fullName, preferredLanguage: 'Ar' }).subscribe({
      next: res => {
        this.loaderService.hide();
        if (res.status !== 'success' || !res.data) {
          this.errorModalService.show(res.message ?? 'تعذّر إنشاء الحساب.', { variant: 'error' });
          return;
        }

        this.tenantService.refresh().subscribe();
        this.authService.fetchMyProfile().subscribe();
        this.router.navigate(['/dashboard']);
      },
      error: err => {
        this.loaderService.hide();
        applyFieldErrors(this.form, err);
        this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر إنشاء الحساب. يرجى المحاولة مرة أخرى.'), {
          variant: 'error',
        });
      },
    });
  }

  protected errorMessage(
    controlName:
      | 'firstName'
      | 'lastName'
      | 'username'
      | 'email'
      | 'password'
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
        pattern: 'اسم المستخدم يجب أن يبدأ بحرف ويحتوي على أحرف/أرقام/underscore فقط.',
      },
      email: {
        required: 'البريد الإلكتروني مطلوب.',
        email: 'أدخل بريدًا إلكترونيًا صحيحًا.',
      },
      password: {
        required: 'كلمة المرور مطلوبة.',
        minlength: 'كلمة المرور يجب أن تكون 8 أحرف على الأقل.',
      },
      confirmPassword: {
        required: 'تأكيد كلمة المرور مطلوب.',
        minlength: 'تأكيد كلمة المرور يجب أن يكون 8 أحرف على الأقل.',
      },
      termsAccepted: { requiredTrue: 'يجب الموافقة على الشروط والأحكام للمتابعة.' },
    } as const;

    return this.formErrorsService.getControlErrorMessage(control, this.submitted(), messagesByControl[controlName]);
  }

  protected passwordsMismatchMessage(): string | null {
    const { password, confirmPassword } = this.form.getRawValue();
    return this.formErrorsService.getPasswordsMismatchMessage(password, confirmPassword, this.submitted());
  }
}
