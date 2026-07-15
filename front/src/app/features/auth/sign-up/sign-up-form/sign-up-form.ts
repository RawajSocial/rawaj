import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { FormErrorsService } from '../../../../services/form-errors.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { translateAuthError } from '../../../../core/auth/auth-error-messages';

@Component({
  selector: 'app-sign-up-form',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './sign-up-form.html',
  styleUrl: './sign-up-form.css',
})
export class SignUpForm {
  protected readonly form;

  protected submitted = false;
  protected submitting = signal(false);
  protected serverError = signal<string | null>(null);

  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);

  constructor(
    private readonly fb: FormBuilder,
    private readonly formErrorsService: FormErrorsService,
  ) {
    this.form = this.fb.nonNullable.group({
      firstName: ['', [Validators.required]],
      lastName: ['', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      businessName: ['', [Validators.required]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required, Validators.minLength(8)]],
      termsAccepted: [false, [Validators.requiredTrue]],
    });
  }

  protected onSubmit(): void {
    this.submitted = true;
    this.serverError.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { firstName, lastName, email, businessName, password } = this.form.getRawValue();
    this.submitting.set(true);

    this.authService
      .register({ email, password, fullName: `${firstName} ${lastName}`.trim(), preferredLanguage: 'Ar' })
      .subscribe({
        next: () => {
          this.submitting.set(false);
          localStorage.setItem('rawaj.account-setup', JSON.stringify({ businessName }));
          this.router.navigate(['/account-setup']);
        },
        error: (error: unknown) => {
          this.submitting.set(false);
          this.serverError.set(translateAuthError(error, 'حدث خطأ غير متوقع، حاول مرة أخرى.'));
        },
      });
  }

  protected errorMessage(
    controlName:
      | 'firstName'
      | 'lastName'
      | 'email'
      | 'businessName'
      | 'password'
      | 'confirmPassword'
      | 'termsAccepted',
  ): string | null {
    const control = this.form.controls[controlName];

    const messagesByControl = {
      firstName: { required: 'الاسم الأول مطلوب.' },
      lastName: { required: 'الاسم الأخير مطلوب.' },
      email: {
        required: 'البريد الإلكتروني مطلوب.',
        email: 'أدخل بريدًا إلكترونيًا صحيحًا.',
      },
      businessName: { required: 'اسم النشاط التجاري مطلوب.' },
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

    return this.formErrorsService.getControlErrorMessage(control, this.submitted, messagesByControl[controlName]);
  }

  protected passwordsMismatchMessage(): string | null {
    const { password, confirmPassword } = this.form.getRawValue();
    return this.formErrorsService.getPasswordsMismatchMessage(password, confirmPassword, this.submitted);
  }
}
