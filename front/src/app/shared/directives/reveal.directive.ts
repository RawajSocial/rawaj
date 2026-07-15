import { isPlatformBrowser } from '@angular/common';
import { Directive, ElementRef, OnDestroy, OnInit, PLATFORM_ID, effect, inject, input } from '@angular/core';
import { RevealObserverService } from '../../services/reveal-observer.service';

type RevealVariant =
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

@Directive({
  selector: '[appReveal]',
  standalone: true,
})
export class RevealDirective implements OnInit, OnDestroy {
  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly observer = inject(RevealObserverService);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  readonly variant = input<RevealVariant>('fade-up', { alias: 'appReveal' });
  readonly revealDuration = input<number>(500, { alias: 'revealDuration' });
  readonly revealDelay = input<number>(0, { alias: 'revealDelay' });

  private currentVariant: RevealVariant | null = null;
  private isVisible = false;

  constructor() {
    effect(() => {
      const element = this.elementRef.nativeElement;
      const variant = this.variant();
      const duration = Math.max(0, this.revealDuration());
      const delay = Math.max(0, this.revealDelay());

      if (this.currentVariant && this.currentVariant !== variant) {
        element.classList.remove(`reveal--${this.currentVariant}`);
      }

      element.classList.add('reveal', `reveal--${variant}`);
      this.currentVariant = variant;

      element.style.setProperty('--reveal-duration', `${duration}ms`);
      element.style.setProperty('--reveal-delay', `${delay}ms`);
    });
  }

  ngOnInit(): void {
    const element = this.elementRef.nativeElement;
    if (!this.isBrowser) {
      element.classList.add('is-visible');
      this.isVisible = true;
      return;
    }

    this.observer.observe(element, () => {
      if (this.isVisible) {
        return;
      }

      this.isVisible = true;
      element.classList.add('is-visible');
    });
  }

  ngOnDestroy(): void {
    if (!this.isBrowser) {
      return;
    }

    this.observer.unobserve(this.elementRef.nativeElement);
  }
}
