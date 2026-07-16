import { isPlatformBrowser } from '@angular/common';
import { AfterViewInit, Directive, ElementRef, OnDestroy, PLATFORM_ID, inject, input } from '@angular/core';
import { gsap } from 'gsap';
import { ScrollTrigger } from 'gsap/ScrollTrigger';

export type GsapRevealVariant =
  | 'fade-up'
  | 'fade-down'
  | 'fade-left'
  | 'fade-right'
  | 'fade-up-far'
  | 'fade-down-far'
  | 'fade-left-far'
  | 'fade-right-far'
  | 'fade-in'
  | 'fade-out';

const OFFSETS: Record<GsapRevealVariant, { x?: number; y?: number }> = {
  'fade-up': { y: 50 },
  'fade-down': { y: -50 },
  'fade-left': { x: 50 },
  'fade-right': { x: -50 },
  'fade-up-far': { y: 100 },
  'fade-down-far': { y: -100 },
  'fade-left-far': { x: 100 },
  'fade-right-far': { x: -100 },
  'fade-in': {},
  'fade-out': {},
};

let scrollTriggerRegistered = false;

/**
 * GSAP + ScrollTrigger powered replacement for the old IntersectionObserver
 * based reveal directive. Same template API (variant + duration in ms) so
 * every call site only needed a mechanical rename.
 */
@Directive({
  selector: '[appGsapReveal]',
  standalone: true,
})
export class GsapRevealDirective implements AfterViewInit, OnDestroy {
  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  readonly variant = input<GsapRevealVariant>('fade-up', { alias: 'appGsapReveal' });
  readonly gsapDuration = input<number>(500, { alias: 'gsapDuration' });
  readonly gsapDelay = input<number>(0, { alias: 'gsapDelay' });

  private tween?: gsap.core.Tween;

  ngAfterViewInit(): void {
    if (!this.isBrowser) {
      return;
    }

    if (typeof window.matchMedia === 'function' && window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      return;
    }

    if (!scrollTriggerRegistered) {
      gsap.registerPlugin(ScrollTrigger);
      scrollTriggerRegistered = true;
    }

    const el = this.elementRef.nativeElement;
    const offset = OFFSETS[this.variant()];
    const duration = Math.max(0, this.gsapDuration()) / 1000;
    const delay = Math.max(0, this.gsapDelay()) / 1000;
    const scrollTrigger: ScrollTrigger.Vars = { trigger: el, start: 'top 88%', once: true };

    this.tween =
      this.variant() === 'fade-out'
        ? gsap.to(el, { opacity: 0, duration, delay, ease: 'power3.out', scrollTrigger })
        : gsap.from(el, { opacity: 0, x: offset.x ?? 0, y: offset.y ?? 0, duration, delay, ease: 'power3.out', scrollTrigger });
  }

  ngOnDestroy(): void {
    this.tween?.scrollTrigger?.kill();
    this.tween?.kill();
  }
}
