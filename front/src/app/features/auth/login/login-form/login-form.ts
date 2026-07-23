import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormErrorsService } from '../../../../services/form-errors.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { TenantService } from '../../../../core/tenant/tenant.service';
import { LoaderService } from '../../../../services/loader.service';
import { applyAuthFailure, applyFieldErrors, hasFieldErrors } from '../../../../core/auth/api-error.util';

/** Backend `Result.Failure` message (LoginCommandHandler) — shown inline under the password field
 *  instead of a generic banner, matching the common "incorrect password" login UX pattern. */
const LOGIN_FAILURE_FIELD_MAP: Record<string, string> = {
  'Invalid email/username or password.': 'password',
};

@Component({
  selector: 'app-login-form',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login-form.html',
  styleUrl: './login-form.css',
})
export class LoginForm {
  protected readonly form;
  protected readonly submitted = signal(false);
  /** Non-field failure message (e.g. network error) shown as a small inline banner — never a modal. */
  protected readonly formError = signal<string | null>(null);

  private readonly authService = inject(AuthService);
  private readonly tenantService = inject(TenantService);
  private readonly loaderService = inject(LoaderService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  constructor(
    private readonly fb: FormBuilder,
    private readonly formErrorsService: FormErrorsService,
  ) {
    this.form = this.fb.nonNullable.group({
      // Login only requires a non-empty password server-side (LoginCommandValidator) — no
      // minlength here, since an existing account's password could pre-date any length rule.
      identifier: ['', [Validators.required]],
      password: ['', [Validators.required]],
      rememberMe: [false],
    });
  }

  protected onSubmit(): void {
    this.submitted.set(true);
    this.formError.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { identifier, password } = this.form.getRawValue();

    this.loaderService.show();
    this.authService.login({ identifier, password }).subscribe({
      next: res => {
        this.loaderService.hide();
        if (res.status !== 'success' || !res.data) {
          this.formError.set(res.message ?? 'تعذّر تسجيل الدخول. يرجى المحاولة مرة أخرى.');
          return;
        }
        this.tenantService.refreshMemberships().subscribe();
        this.authService.fetchMyProfile().subscribe();
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
        this.router.navigateByUrl(returnUrl ?? '/dashboard');
      },
      error: err => {
        this.loaderService.hide();
        applyFieldErrors(this.form, err);
        // Field-level errors already show inline under their control. Anything else (invalid
        // credentials, network/server errors) is resolved to an inline field message or a small
        // banner by applyAuthFailure — never a popup modal.
        if (!hasFieldErrors(err)) {
          this.formError.set(
            applyAuthFailure(this.form, err, LOGIN_FAILURE_FIELD_MAP, 'تعذّر تسجيل الدخول. يرجى المحاولة مرة أخرى.'),
          );
        }
      },
    });
  }

  protected errorMessage(controlName: 'identifier' | 'password'): string | null {
    if (controlName === 'identifier') {
      return this.formErrorsService.getControlErrorMessage(this.form.controls.identifier, this.submitted(), {
        required: 'البريد الإلكتروني أو اسم المستخدم مطلوب.',
      });
    }

    return this.formErrorsService.getControlErrorMessage(this.form.controls.password, this.submitted(), {
      required: 'كلمة المرور مطلوبة.',
    });
  }
}
