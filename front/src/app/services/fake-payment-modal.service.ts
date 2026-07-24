import { Injectable, signal } from '@angular/core';

export interface FakePaymentOptions {
  title: string;
  /** e.g. "$249.00 / شهرياً" or "$50.00". */
  priceLabel: string;
  confirmLabel?: string;
}

export interface FakePaymentState {
  open: boolean;
  title: string;
  priceLabel: string;
  confirmLabel: string;
}

const INITIAL_STATE: FakePaymentState = { open: false, title: '', priceLabel: '', confirmLabel: 'تأكيد الدفع' };

/**
 * Central place to trigger the app-wide fake-payment confirmation modal, mirroring
 * `ConfirmDialogService`. Every purchase in the app (coin packages, add-ons, plan changes) goes
 * through this one dialog instead of each page building its own — there is no real payment
 * gateway behind it: no card fields, no validation, it always "succeeds" once confirmed.
 */
@Injectable({ providedIn: 'root' })
export class FakePaymentModalService {
  private readonly _state = signal<FakePaymentState>(INITIAL_STATE);
  readonly state = this._state.asReadonly();
  private resolver: ((result: boolean) => void) | null = null;

  confirm(options: FakePaymentOptions): Promise<boolean> {
    this._state.set({
      open: true,
      title: options.title,
      priceLabel: options.priceLabel,
      confirmLabel: options.confirmLabel ?? INITIAL_STATE.confirmLabel,
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
