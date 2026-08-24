import { Injectable } from '@angular/core';
import { AbstractControl } from '@angular/forms';

type ControlErrorMessages = {
  required?: string;
  email?: string;
  minlength?: string;
  maxlength?: string;
  pattern?: string;
  requiredTrue?: string;
  hasUppercase?: string;
  hasLowercase?: string;
  hasDigit?: string;
  hasSpecialChar?: string;
  invalidUrl?: string;
};

@Injectable({ providedIn: 'root' })
export class FormErrorsService {
  getControlErrorMessage(
    control: AbstractControl | null,
    submitted: boolean,
    messages: ControlErrorMessages = {},
  ): string | null {
    // `dirty` flips true on the very first keystroke (unlike `touched`, which needs a blur), so
    // this reports validity live as the user types instead of only after they leave the field.
    if (!control || !control.invalid || (!control.dirty && !submitted)) {
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

    if (control.errors?.['hasUppercase']) {
      return messages.hasUppercase ?? 'يجب أن تحتوي على حرف كبير واحد على الأقل.';
    }

    if (control.errors?.['hasLowercase']) {
      return messages.hasLowercase ?? 'يجب أن تحتوي على حرف صغير واحد على الأقل.';
    }

    if (control.errors?.['hasDigit']) {
      return messages.hasDigit ?? 'يجب أن تحتوي على رقم واحد على الأقل.';
    }

    if (control.errors?.['hasSpecialChar']) {
      return messages.hasSpecialChar ?? 'يجب أن تحتوي على رمز خاص واحد على الأقل.';
    }

    if (control.errors?.['invalidUrl']) {
      return messages.invalidUrl ?? 'أدخل رابطًا صحيحًا، مثل example.com أو https://example.com.';
    }

    return 'البيانات المدخلة غير صالحة.';
  }

  getPasswordsMismatchMessage(
    password: string,
    confirmPassword: string,
    submitted: boolean,
  ): string | null {
    // Live as soon as the user has typed something into confirmPassword (not just on submit) —
    // the confirmPassword length check already prevents it firing while that field is still empty.
    if ((!submitted && confirmPassword.length === 0) || password.length === 0) {
      return null;
    }

    if (password !== confirmPassword) {
      return 'كلمتا المرور غير متطابقتين.';
    }

    return null;
  }
}
