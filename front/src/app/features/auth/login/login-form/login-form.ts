import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { FormErrorsService } from '../../../../services/form-errors.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { ErrorModalService } from '../../../../services/error-modal.service';
import { LoaderService } from '../../../../services/loader.service';
import { extractApiErrorMessage, applyFieldErrors } from '../../../../core/auth/api-error.util';

@Component({
  selector: 'app-login-form',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login-form.html',
  styleUrl: './login-form.css',
})
export class LoginForm {
  protected readonly form;
  protected readonly submitted = signal(false);

  private readonly authService = inject(AuthService);
  private readonly errorModalService = inject(ErrorModalService);
  private readonly loaderService = inject(LoaderService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

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
    this.submitted.set(true);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { email, password } = this.form.getRawValue();

    this.loaderService.show();
    this.authService.login({ email, password }).subscribe({
      next: res => {
        this.loaderService.hide();
        if (res.status !== 'success' || !res.data) {
          this.errorModalService.show(res.message ?? 'تعذّر تسجيل الدخول.', { variant: 'error' });
          return;
        }
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
        this.router.navigateByUrl(returnUrl ?? '/dashboard');
      },
      error: err => {
        this.loaderService.hide();
        applyFieldErrors(this.form, err);
        this.errorModalService.show(extractApiErrorMessage(err, 'البريد الإلكتروني أو كلمة المرور غير صحيحة.'), {
          variant: 'error',
        });
      },
    });
  }

  protected errorMessage(controlName: 'email' | 'password'): string | null {
    if (controlName === 'email') {
      return this.formErrorsService.getControlErrorMessage(this.form.controls.email, this.submitted(), {
        required: 'البريد الإلكتروني مطلوب.',
        email: 'أدخل بريدًا إلكترونيًا صحيحًا.',
      });
    }

    return this.formErrorsService.getControlErrorMessage(this.form.controls.password, this.submitted(), {
      required: 'كلمة المرور مطلوبة.',
      minlength: 'كلمة المرور يجب أن تكون 8 أحرف على الأقل.',
    });
  }
}
