import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { FormErrorsService } from '../../../../services/form-errors.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { TenantService } from '../../../../core/tenant/tenant.service';
import { translateAuthError } from '../../../../core/auth/auth-error-messages';

@Component({
  selector: 'app-login-form',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login-form.html',
  styleUrl: './login-form.css',
})
export class LoginForm {
  protected readonly form;

  protected submitted = false;
  protected submitting = signal(false);
  protected serverError = signal<string | null>(null);

  private readonly authService = inject(AuthService);
  private readonly tenantService = inject(TenantService);
  private readonly router = inject(Router);

  constructor(
    private readonly fb: FormBuilder,
    private readonly formErrorsService: FormErrorsService,
  ) {
    this.form = this.fb.nonNullable.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      rememberMe: [false],
    });
  }

  protected onSubmit(): void {
    this.submitted = true;
    this.serverError.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { email, password } = this.form.getRawValue();
    this.submitting.set(true);

    this.authService.login({ email, password }).subscribe({
      next: () => {
        // Whether this user owns/belongs to a tenant or not, they land on the dashboard - it
        // shows a "create your business" prompt for tenant-less accounts instead of forcing
        // everyone through setup immediately after logging in.
        this.tenantService.loadContext().subscribe({
          next: () => {
            this.submitting.set(false);
            this.router.navigate(['/dashboard']);
          },
          error: () => {
            this.submitting.set(false);
            this.router.navigate(['/dashboard']);
          },
        });
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        this.serverError.set(translateAuthError(error, 'حدث خطأ غير متوقع، حاول مرة أخرى.'));
      },
    });
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
