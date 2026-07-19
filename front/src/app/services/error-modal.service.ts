import { Injectable, signal } from '@angular/core';

export type ErrorModalVariant = 'error' | 'warning' | 'info' | 'success';

export interface ErrorModalState {
  open: boolean;
  title: string;
  message: string;
  icon: string;
  variant: ErrorModalVariant;
}

export interface ErrorModalOptions {
  title?: string;
  icon?: string;
  variant?: ErrorModalVariant;
}

const DEFAULT_ICON: Record<ErrorModalVariant, string> = {
  error: 'fa-solid fa-circle-exclamation',
  warning: 'fa-solid fa-triangle-exclamation',
  info: 'fa-solid fa-circle-info',
  success: 'fa-solid fa-circle-check',
};

const DEFAULT_TITLE: Record<ErrorModalVariant, string> = {
  error: 'حدث خطأ',
  warning: 'تنبيه',
  info: 'معلومة',
  success: 'تم بنجاح',
};

const INITIAL_STATE: ErrorModalState = {
  open: false,
  title: '',
  message: '',
  icon: DEFAULT_ICON.error,
  variant: 'error',
};

/**
 * Central place to trigger the app-wide error/notice modal. Any component can
 * inject this service and call `show()` instead of owning its own modal
 * markup/state, so the dialog UI only exists once in `ErrorModal`.
 */
@Injectable({ providedIn: 'root' })
export class ErrorModalService {
  private readonly _state = signal<ErrorModalState>(INITIAL_STATE);
  readonly state = this._state.asReadonly();

  show(message: string, options: ErrorModalOptions = {}): void {
    const variant = options.variant ?? 'error';
    this._state.set({
      open: true,
      message,
      variant,
      title: options.title ?? DEFAULT_TITLE[variant],
      icon: options.icon ?? DEFAULT_ICON[variant],
    });
  }

  close(): void {
    this._state.update(s => ({ ...s, open: false }));
  }
}
