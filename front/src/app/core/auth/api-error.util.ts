import { HttpErrorResponse } from '@angular/common/http';
import { FormGroup } from '@angular/forms';
import { ApiResponse } from '../../model/auth.model';

/** Maps a backend FluentValidation field name (PascalCase) to its Angular form control (camelCase). */
function toControlName(fieldName: string): string {
  return fieldName.charAt(0).toLowerCase() + fieldName.slice(1);
}

/** Reads the backend's `ApiResponse.message` out of a failed HttpClient request, falling back for network errors. */
export function extractApiErrorMessage(err: unknown, fallback: string): string {
  if (err instanceof HttpErrorResponse) {
    if (err.status === 0) {
      return 'تعذّر الاتصال بالخادم. تحقّق من اتصالك بالإنترنت وحاول مرة أخرى.';
    }
    const body = err.error as ApiResponse<unknown> | undefined;
    if (body?.message) return body.message;
  }
  return fallback;
}

/**
 * When the backend rejects a request with a 400 + field-level validation
 * errors (see `ApiResponse.errors`), pushes each message onto the matching
 * form control so `FormErrorsService.getControlErrorMessage` can surface it.
 */
export function applyFieldErrors(form: FormGroup, err: unknown): void {
  if (!(err instanceof HttpErrorResponse)) return;

  const body = err.error as ApiResponse<unknown> | undefined;
  if (!body?.errors) return;

  for (const [field, messages] of Object.entries(body.errors)) {
    const control = form.get(toControlName(field));
    if (control && messages.length > 0) {
      control.setErrors({ ...control.errors, serverMessage: messages[0] });
    }
  }
}
