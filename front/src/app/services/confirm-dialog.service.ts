import { Injectable, signal } from '@angular/core';

export interface ConfirmDialogOptions {
  title?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  variant?: 'danger' | 'default';
}

export interface ConfirmDialogState {
  open: boolean;
  message: string;
  title: string;
  confirmLabel: string;
  cancelLabel: string;
  variant: 'danger' | 'default';
}

const INITIAL_STATE: ConfirmDialogState = {
  open: false,
  message: '',
  title: 'تأكيد',
  confirmLabel: 'تأكيد',
  cancelLabel: 'إلغاء',
  variant: 'default',
};

/**
 * Central place to trigger a Yes/No confirmation dialog, mirroring `ErrorModalService`.
 * `confirm()` resolves once the user responds (or dismisses the dialog, treated as cancel),
 * so callers can `await` it inline before proceeding with a destructive action.
 */
@Injectable({ providedIn: 'root' })
export class ConfirmDialogService {
  private readonly _state = signal<ConfirmDialogState>(INITIAL_STATE);
  readonly state = this._state.asReadonly();
  private resolver: ((result: boolean) => void) | null = null;

  confirm(message: string, options: ConfirmDialogOptions = {}): Promise<boolean> {
    this._state.set({
      open: true,
      message,
      title: options.title ?? INITIAL_STATE.title,
      confirmLabel: options.confirmLabel ?? INITIAL_STATE.confirmLabel,
      cancelLabel: options.cancelLabel ?? INITIAL_STATE.cancelLabel,
      variant: options.variant ?? INITIAL_STATE.variant,
    });
    return new Promise(resolve => {
      this.resolver = resolve;
    });
  }

  respond(result: boolean): void {
    this._state.update(s => ({ ...s, open: false }));
    this.resolver?.(result);
    this.resolver = null;
  }
}
