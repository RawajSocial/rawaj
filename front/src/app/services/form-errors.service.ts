import { Injectable } from '@angular/core';
import { AbstractControl } from '@angular/forms';

type ControlErrorMessages = {
  required?: string;
  email?: string;
  minlength?: string;
  maxlength?: string;
  pattern?: string;
  requiredTrue?: string;
};

@Injectable({ providedIn: 'root' })
export class FormErrorsService {
  getControlErrorMessage(
    control: AbstractControl | null,
    submitted: boolean,
    messages: ControlErrorMessages = {},
  ): string | null {
    if (!control || !control.invalid || (!control.touched && !submitted)) {
      return null;
    }

    if (control.errors?.['serverMessage']) {
      return control.errors['serverMessage'] as string;
    }

    if (control.errors?.['required']) {
      return messages.required ?? 'هذا الحقل مطلوب.';
    }

    if (control.errors?.['email']) {
      return messages.email ?? 'أدخل بريدًا إلكترونيًا صحيحًا.';
    }

    if (control.errors?.['minlength']) {
      const requiredLength = control.errors['minlength'].requiredLength as number;
      return messages.minlength ?? `يجب إدخال ${requiredLength} أحرف على الأقل.`;
    }

    if (control.errors?.['maxlength']) {
      const requiredLength = control.errors['maxlength'].requiredLength as number;
      return messages.maxlength ?? `يجب ألا يتجاوز ${requiredLength} حرفًا.`;
    }

    if (control.errors?.['pattern']) {
      return messages.pattern ?? 'صيغة البيانات المدخلة غير صحيحة.';
    }

    if (control.errors?.['requiredTrue']) {
      return messages.requiredTrue ?? 'يجب الموافقة للمتابعة.';
    }

    return 'البيانات المدخلة غير صالحة.';
  }

  getPasswordsMismatchMessage(
    password: string,
    confirmPassword: string,
    submitted: boolean,
  ): string | null {
    if (!submitted || password.length === 0 || confirmPassword.length === 0) {
      return null;
    }

    if (password !== confirmPassword) {
      return 'كلمتا المرور غير متطابقتين.';
    }

    return null;
  }
}
