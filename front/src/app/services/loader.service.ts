import { Injectable, computed, signal } from '@angular/core';

/**
 * Tracks how many concurrent operations want the page loader visible, so
 * overlapping `show()`/`hide()` calls (e.g. a route transition finishing
 * while a manual request is still pending) don't hide it prematurely.
 */
@Injectable({ providedIn: 'root' })
export class LoaderService {
  private readonly _pending = signal(0);
  readonly loading = computed(() => this._pending() > 0);

  show(): void {
    this._pending.update(n => n + 1);
  }

  hide(): void {
    this._pending.update(n => Math.max(0, n - 1));
  }
}
