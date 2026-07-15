import { ApiError } from '../api';

/**
 * The backend returns business-rule failure messages in English (e.g. "Invalid email or
 * password."). The UI is fully Arabic, so raw backend text reads as broken to users. This maps
 * the known auth failure messages to Arabic and falls back to a generic Arabic message for
 * anything unrecognized (including network errors) rather than ever showing English/raw text.
 */
const KNOWN_MESSAGES: Record<string, string> = {
  'invalid email or password.': 'البريد الإلكتروني أو كلمة المرور غير صحيحة.',
  'a user with this email already exists.': 'يوجد حساب مسجّل بهذا البريد الإلكتروني بالفعل.',
};

export function translateAuthError(error: unknown, fallback: string): string {
  if (!(error instanceof ApiError)) {
    return fallback;
  }

  if (error.httpStatus === 0 || error.message === 'Network error. Please try again.') {
    return 'تعذر الاتصال بالخادم. تحقق من اتصالك بالإنترنت وحاول مرة أخرى.';
  }

  const translated = KNOWN_MESSAGES[error.message.trim().toLowerCase()];
  return translated ?? fallback;
}
