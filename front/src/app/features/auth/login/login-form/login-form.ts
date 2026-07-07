import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormErrorsService } from '../../../../services/form-errors.service';

@Component({
  selector: 'app-login-form',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login-form.html',
  styleUrl: './login-form.css',
})
export class LoginForm {
  protected readonly form;

  protected submitted = false;

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
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    console.log('Login payload', this.form.getRawValue());
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
