import { Validators } from '@angular/forms';

/** Mirrors the backend's `Rawaj.Application.Common.Validation.UsernameRules`. */
export const usernameValidators = [
  Validators.required,
  Validators.minLength(3),
  Validators.maxLength(30),
  Validators.pattern(/^[a-zA-Z][a-zA-Z0-9_]*$/),
];
