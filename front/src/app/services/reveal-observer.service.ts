import { Injectable, NgZone, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

type RevealCallback = () => void;

@Injectable({
  providedIn: 'root',
})
export class RevealObserverService {
  private readonly zone = inject(NgZone);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);
  private readonly supportsObserver = this.isBrowser && typeof IntersectionObserver !== 'undefined';

  private observer: IntersectionObserver | null = null;
  private readonly callbacks = new Map<Element, RevealCallback>();

  observe(element: Element, onEnter: RevealCallback): void {
    if (!this.supportsObserver) {
      onEnter();
      return;
    }

    this.callbacks.set(element, onEnter);
    this.ensureObserver().observe(element);
  }

  unobserve(element: Element): void {
    this.callbacks.delete(element);
    this.observer?.unobserve(element);
  }

  private ensureObserver(): IntersectionObserver {
    if (this.observer) {
      return this.observer;
    }

    this.observer = this.zone.runOutsideAngular(
      () =>
        new IntersectionObserver(
          (entries) => {
            for (const entry of entries) {
              if (!entry.isIntersecting) {
                continue;
              }

              const callback = this.callbacks.get(entry.target);
              if (!callback) {
                continue;
              }

              this.callbacks.delete(entry.target);
              this.observer?.unobserve(entry.target);
              callback();
            }
          },
          {
            threshold: 0.2,
          }
        )
    );

    return this.observer;
  }
}
