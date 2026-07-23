import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/** Mirrors the backend's `Rawaj.Application.Common.Validation.UrlNormalizer` — a bare domain like
 *  "example.com" or "www.example.com" is accepted (an "https://" scheme is assumed) since that's
 *  the natural way most people type a website; only genuinely malformed values are rejected. */
export function ensureUrlScheme(value: string): string {
  const trimmed = value.trim();
  return trimmed.includes('://') ? trimmed : `https://${trimmed}`;
}

export function isValidUrlLike(value: string): boolean {
  try {
    new URL(ensureUrlScheme(value));
    return true;
  } catch {
    return false;
  }
}

/** Optional URL field validator — empty is valid (field is optional), matching the backend's
 *  `.When(x => !string.IsNullOrWhiteSpace(...))` guard on the same rule. */
export const urlValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const value = (control.value ?? '').trim();
  if (!value) return null;
  return isValidUrlLike(value) ? null : { invalidUrl: true };
};
