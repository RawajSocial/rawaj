import { Injectable, signal } from '@angular/core';

export interface CelebrationModalState {
  open: boolean;
  title: string;
  message: string;
}

const INITIAL_STATE: CelebrationModalState = { open: false, title: '', message: '' };

/**
 * Central place to trigger the app-wide celebration modal (confetti burst + animated checkmark)
 * for reward/achievement moments — e.g. the activation coin reward. Mirrors ErrorModalService's
 * pattern: one signal-backed state, one globally-mounted component in app.html.
 */
@Injectable({ providedIn: 'root' })
export class CelebrationModalService {
  private readonly _state = signal<CelebrationModalState>(INITIAL_STATE);
  readonly state = this._state.asReadonly();

  show(message: string, title = 'مبروك!'): void {
    this._state.set({ open: true, title, message });
  }

  close(): void {
    this._state.update(s => ({ ...s, open: false }));
  }
}
