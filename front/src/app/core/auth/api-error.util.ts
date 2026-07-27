import { HttpErrorResponse } from '@angular/common/http';
import { FormGroup } from '@angular/forms';
import { ApiResponse } from '../../model/auth.model';

/** Maps a backend FluentValidation field name (PascalCase) to its Angular form control (camelCase). */
function toControlName(fieldName: string): string {
  return fieldName.charAt(0).toLowerCase() + fieldName.slice(1);
}

/**
 * The backend's validation/auth messages are English by convention (see other
 * FluentValidation rules in the codebase) — this is the one place that translates the exact,
 * known strings into friendly Arabic so the UI never shows raw backend text. Any message not
 * listed here falls back to a safe generic Arabic message instead of leaking the English original.
 * Keep this in sync with RegisterCommandValidator, UsernameRules, RegisterCommandHandler, and
 * LoginCommandHandler's literal message text.
 */
const KNOWN_MESSAGE_TRANSLATIONS: Record<string, string> = {
  'Password is required.': 'كلمة المرور مطلوبة.',
  'Password must be at least 8 characters.': 'يجب أن تتكوّن كلمة المرور من 8 أحرف على الأقل.',
  'Password must contain at least one uppercase letter.': 'يجب أن تحتوي كلمة المرور على حرف كبير واحد على الأقل.',
  'Password must contain at least one lowercase letter.': 'يجب أن تحتوي كلمة المرور على حرف صغير واحد على الأقل.',
  'Password must contain at least one digit.': 'يجب أن تحتوي كلمة المرور على رقم واحد على الأقل.',
  'Password must contain at least one special character.': 'يجب أن تحتوي كلمة المرور على رمز خاص واحد على الأقل.',
  'Username must start with a letter and contain only letters, numbers, or underscores.':
    'يجب أن يبدأ اسم المستخدم بحرف، ويحتوي على أحرف وأرقام وشرطة سفلية (_) فقط.',
  'A user with this email already exists.': 'هذا البريد الإلكتروني مستخدم بالفعل.',
  'A user with this username already exists.': 'اسم المستخدم هذا مستخدم بالفعل.',
  'Invalid email/username or password.': 'البريد الإلكتروني أو اسم المستخدم أو كلمة المرور غير صحيحة.',

  // ── Campaigns (CreateCampaignCommandValidator, UpdateCampaignCommandValidator,
  //    GenerateCampaignContentCommandValidator, UpdateCampaignCommandHandler) ──
  'Campaign name is required.': 'اسم الحملة مطلوب.',
  'Campaign name must be 255 characters or fewer.': 'يجب ألا يتجاوز اسم الحملة 255 حرفًا.',
  'Objective must be 255 characters or fewer.': 'يجب ألا يتجاوز هدف الحملة 255 حرفًا.',
  'Budget cannot be negative.': 'لا يمكن أن تكون الميزانية بالسالب.',
  'At least one target platform is required.': 'اختر منصة واحدة على الأقل.',
  'End date must be on or after the start date.': 'يجب أن يكون تاريخ النهاية في نفس تاريخ البداية أو بعده.',
  'Post count must be between 1 and 15.': 'يجب أن يكون عدد المنشورات بين 1 و15.',
  'Generate a strategy before approving it.': 'يجب توليد الاستراتيجية قبل اعتمادها.',
  'Only an archived campaign can be restored.': 'يمكن استعادة الحملات المؤرشفة فقط.',
  'Campaign not found.': 'لم يتم العثور على الحملة.',
};

/** The generic top-level message the backend attaches to every FluentValidation failure (see
 *  ExceptionHandlingMiddleware). On its own it tells the user nothing, which is why
 *  `extractApiErrorMessage` digs into `errors` when it sees this. */
const GENERIC_VALIDATION_MESSAGE = 'Validation failed.';

const GENERIC_FIELD_MESSAGE = 'يرجى التحقق من صحة هذا الحقل.';

function translate(message: string): string {
  return KNOWN_MESSAGE_TRANSLATIONS[message] ?? GENERIC_FIELD_MESSAGE;
}

/**
 * Reads the backend's `ApiResponse.message` out of a failed HttpClient request, falling back for
 * network errors.
 *
 * When the failure is a FluentValidation rejection, the top-level message is always the useless
 * literal "Validation failed." and the real reason lives in `errors` — which this used to ignore
 * entirely, so every field-level rule (a post count out of range, an end date before its start,
 * a campaign name that's too long) surfaced to the user as "Validation failed." with no clue what
 * to change. Callers with a form should still prefer `applyFieldErrors`, which shows each message
 * under its own input; this is for the ones that only have a modal or a banner to put it in.
 */
export function extractApiErrorMessage(err: unknown, fallback: string): string {
  if (err instanceof HttpErrorResponse) {
    if (err.status === 0) {
      return 'تعذّر الاتصال بالخادم. تحقّق من اتصالك بالإنترنت وحاول مرة أخرى.';
    }
    const body = err.error as ApiResponse<unknown> | undefined;

    if (body?.message === GENERIC_VALIDATION_MESSAGE) {
      const fieldMessage = firstTranslatableFieldMessage(body.errors);
      if (fieldMessage) return fieldMessage;
    }

    if (body?.message) return body.message;
  }
  return fallback;
}

/**
 * The first field-level message that has a known Arabic translation. Untranslated ones are skipped
 * rather than shown: they're raw English FluentValidation defaults, and the codebase's rule is that
 * backend text is never leaked to the user (see `translate`). If nothing matches, the caller's own
 * fallback is used instead of a message the user can't read.
 */
function firstTranslatableFieldMessage(
  errors: Readonly<Record<string, string[]>> | null | undefined,
): string | null {
  if (!errors) return null;
  for (const messages of Object.values(errors)) {
    for (const message of messages) {
      if (KNOWN_MESSAGE_TRANSLATIONS[message]) return KNOWN_MESSAGE_TRANSLATIONS[message];
    }
  }
  return null;
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
      control.setErrors({ ...control.errors, serverMessage: translate(messages[0]) });
    }
  }
}

/**
 * True when the failure carries field-level validation errors (see
 * `ApiResponse.errors`) that `applyFieldErrors` can already show inline.
 * Callers should skip the generic error modal in that case — showing both
 * an inline message under the field and a popup for the same problem is
 * redundant. The modal stays reserved for failures with no field to attach
 * to (invalid credentials, duplicate email, network/server errors).
 */
export function hasFieldErrors(err: unknown): boolean {
  if (!(err instanceof HttpErrorResponse)) return false;
  const body = err.error as ApiResponse<unknown> | undefined;
  return !!body?.errors && Object.keys(body.errors).length > 0;
}

/**
 * Resolves how to present an auth failure that has no field-level errors (see `hasFieldErrors`) —
 * used instead of a popup modal so login/sign-up failures always read inline, like every other
 * validation message on the form. If the backend's top-level `message` is a known one that
 * naturally belongs to a specific control (e.g. "this email already exists" belongs under the
 * email field — pass that mapping via `fieldMap`), it's set there (translated to Arabic) and this
 * returns `null`. Otherwise it returns a translated, Arabic, non-field message meant for a small
 * inline banner near the submit button — never the raw backend text, and never a modal.
 */
export function applyAuthFailure(
  form: FormGroup,
  err: unknown,
  fieldMap: Record<string, string>,
  fallback: string,
): string | null {
  if (err instanceof HttpErrorResponse && err.status === 0) {
    return 'تعذّر الاتصال بالخادم. تحقّق من اتصالك بالإنترنت وحاول مرة أخرى.';
  }

  const body = err instanceof HttpErrorResponse ? (err.error as ApiResponse<unknown> | undefined) : undefined;
  const message = body?.message;
  if (!message || !KNOWN_MESSAGE_TRANSLATIONS[message]) {
    return fallback;
  }

  const controlName = fieldMap[message];
  const control = controlName ? form.get(controlName) : null;
  if (control) {
    control.setErrors({ ...control.errors, serverMessage: translate(message) });
    control.markAsTouched();
    return null;
  }

  return translate(message);
}
