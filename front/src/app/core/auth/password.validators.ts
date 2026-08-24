import { AbstractControl, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';

/** Mirrors the ASP.NET Identity password policy (Rawaj.Persistence/DependencyInjection.cs's
 *  RequiredLength=8 plus Identity's default RequireDigit/RequireLowercase/RequireUppercase/
 *  RequireNonAlphanumeric) and RegisterCommandValidator's matching FluentValidation rules. */
export const PASSWORD_MIN_LENGTH = 8;

function requiresMatch(regex: RegExp, errorKey: string): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null =>
    regex.test(control.value ?? '') ? null : { [errorKey]: true };
}

export const passwordValidators: ValidatorFn[] = [
  Validators.required,
  Validators.minLength(PASSWORD_MIN_LENGTH),
  requiresMatch(/[A-Z]/, 'hasUppercase'),
  requiresMatch(/[a-z]/, 'hasLowercase'),
  requiresMatch(/[0-9]/, 'hasDigit'),
  requiresMatch(/[^a-zA-Z0-9]/, 'hasSpecialChar'),
];
