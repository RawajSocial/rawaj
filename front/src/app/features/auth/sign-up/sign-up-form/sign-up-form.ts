import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { FormErrorsService } from '../../../../services/form-errors.service';

@Component({
  selector: 'app-sign-up-form',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './sign-up-form.html',
  styleUrl: './sign-up-form.css',
})
export class SignUpForm {
  protected readonly form;

  protected readonly submitted = signal(false);

  private readonly router = inject(Router);

  constructor(
    private readonly fb: FormBuilder,
    private readonly formErrorsService: FormErrorsService,
  ) {
    this.form = this.fb.nonNullable.group({
      firstName: ['', [Validators.required]],
      lastName: ['', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      userName: [
        '',
        [Validators.required, Validators.minLength(3), Validators.maxLength(50), Validators.pattern(/^[a-zA-Z0-9_.-]+$/)],
      ],
      businessName: ['', [Validators.required]],
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

    const existing = JSON.parse(localStorage.getItem('rawaj.account-setup') ?? '{}');
    localStorage.setItem(
      'rawaj.account-setup',
      JSON.stringify({ ...existing, email: this.form.value.email, userName: this.form.value.userName }),
    );
    this.router.navigate(['/account-setup']);
  }

  protected errorMessage(
    controlName:
      | 'firstName'
      | 'lastName'
      | 'email'
      | 'userName'
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
      userName: {
        required: 'اسم المستخدم مطلوب.',
        minlength: 'اسم المستخدم يجب أن يكون 3 أحرف على الأقل.',
        maxlength: 'اسم المستخدم يجب ألا يتجاوز 50 حرفًا.',
        pattern: 'اسم المستخدم يمكن أن يحتوي على أحرف وأرقام و . _ - فقط.',
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

    return this.formErrorsService.getControlErrorMessage(control, this.submitted(), messagesByControl[controlName]);
  }

  protected passwordsMismatchMessage(): string | null {
    const { password, confirmPassword } = this.form.getRawValue();
    return this.formErrorsService.getPasswordsMismatchMessage(password, confirmPassword, this.submitted());
  }
}
