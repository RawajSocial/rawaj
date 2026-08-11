import { isPlatformBrowser } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnDestroy,
  PLATFORM_ID,
  afterNextRender,
  inject,
  signal,
  viewChild,
  viewChildren,
} from '@angular/core';
import { gsap } from 'gsap';
import { Draggable } from 'gsap/Draggable';
import { GsapRevealDirective } from '../../../shared/directives/gsap-reveal.directive';

interface Post {
  id: number;
  title: string;
  image: string;
}

const POSTS: Post[] = [
  { id: 1, title: 'إعلان عطر رواج الذكي', image: '/1.png' },
  { id: 2, title: 'تصميم بوست ترويجي متميز لعطورك', image: '/2.png' },
  { id: 3, title: 'حملة تسويقية إبداعية مخصصة للتفاعل', image: '/3.png' },
  { id: 4, title: 'منشورات رقمية احترافية لشبكات التواصل', image: '/4.png' },
  { id: 5, title: 'هوية بصرية استثنائية لصورة علامتك التجارية', image: '/5.png' },
  { id: 6, title: 'أفكار إعلانية مبتكرة وجاذبة للجمهور', image: '/2.png' },
];

const AUTOPLAY_INTERVAL_MS = 3800;
const TRANSITION_DURATION_S = 0.6;
const SWIPE_THRESHOLD_PX = 60;
const NEAR_SCALE = 0.86;
const NEAR_OPACITY = 0.55;
const FAR_SCALE = 0.72;
const FAR_OPACITY = 0.28;
const HIDDEN_OPACITY = 0;

/**
 * GSAP carousel: every card is absolutely centered on the same spot; each
 * frame `layout()` moves every card in one shot to an x-offset/scale/opacity
 * derived purely from its signed distance (with wrap-around) to the active
 * index. Distance 0 = full-size centered card, ±1 and ±2 are progressively
 * smaller/dimmer/blurrier peeks on either side (two per side), anything
 * further is fully hidden. Because GSAP's function-based values recompute
 * per element, there is no separate incoming/outgoing bookkeeping to get
 * wrong.
 */
@Component({
  selector: 'app-posts',
  imports: [GsapRevealDirective],
  templateUrl: './posts.html',
  styleUrl: './posts.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Posts implements OnDestroy {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);
  private readonly viewportRef = viewChild.required<ElementRef<HTMLElement>>('viewport');
  private readonly stageRef = viewChild.required<ElementRef<HTMLElement>>('stage');
  private readonly cardRefs = viewChildren<ElementRef<HTMLElement>>('card');

  readonly slides = POSTS;
  readonly activeIndex = signal(0);

  private peekOffsetPx = 190;
  private peekOffset2Px = 340;
  private draggable: Draggable | null = null;
  private autoplayTimer: ReturnType<typeof setInterval> | null = null;

  constructor() {
    if (this.isBrowser) {
      gsap.registerPlugin(Draggable);
    }
    afterNextRender(() => this.setup());
  }

  ngOnDestroy(): void {
    this.stopAutoplay();
    this.draggable?.kill();
  }

  next(): void {
    this.restartAutoplay();
    this.go(1);
  }

  previous(): void {
    this.restartAutoplay();
    this.go(-1);
  }

  pauseAutoplay(): void {
    this.stopAutoplay();
  }

  resumeAutoplay(): void {
    this.startAutoplay();
  }

  private setup(): void {
    if (!this.isBrowser) return;
    if (!this.cardRefs().length) return;

    const stageStyle = getComputedStyle(this.stageRef().nativeElement);
    const parsed1 = parseFloat(stageStyle.getPropertyValue('--peek-offset'));
    const parsed2 = parseFloat(stageStyle.getPropertyValue('--peek-offset-2'));
    if (!Number.isNaN(parsed1)) this.peekOffsetPx = parsed1;
    if (!Number.isNaN(parsed2)) this.peekOffset2Px = parsed2;

    this.layout(true);

    const viewport = this.viewportRef().nativeElement;
    const proxy = document.createElement('div');
    this.draggable = Draggable.create(proxy, {
      trigger: viewport,
      type: 'x',
      onDragEnd: () => {
        const delta = this.draggable?.x ?? 0;
        gsap.set(proxy, { x: 0 });
        if (Math.abs(delta) > SWIPE_THRESHOLD_PX) {
          this.restartAutoplay();
          this.go(delta < 0 ? 1 : -1);
        }
      },
    })[0];

    this.startAutoplay();
  }

  private go(direction: 1 | -1): void {
    if (!this.isBrowser) return;
    const length = this.slides.length;
    this.activeIndex.set(((this.activeIndex() + direction) % length + length) % length);
    this.layout(false);
  }

  private layout(instant: boolean): void {
    const cards = this.cardRefs().map((r) => r.nativeElement);
    const length = cards.length;
    const active = this.activeIndex();
    const offsetOf = (i: number): number => this.signedOffset(i, active, length);

    gsap.to(cards, {
      x: (i: number) => this.xFor(offsetOf(i)),
      scale: (i: number) => this.scaleFor(offsetOf(i)),
      opacity: (i: number) => this.opacityFor(offsetOf(i)),
      filter: (i: number) => this.blurFor(offsetOf(i)),
      zIndex: (i: number) => this.zIndexFor(offsetOf(i)),
      duration: instant ? 0 : TRANSITION_DURATION_S,
      ease: 'power3.inOut',
      overwrite: true,
    });
  }

  private signedOffset(i: number, active: number, length: number): number {
    let raw = ((i - active) % length + length) % length;
    if (raw > length / 2) raw -= length;
    return raw;
  }

  private xFor(offset: number): number {
    const abs = Math.abs(offset);
    const sign = Math.sign(offset);
    if (abs === 0) return 0;
    if (abs === 1) return sign * this.peekOffsetPx;
    if (abs === 2) return sign * this.peekOffset2Px;
    return sign * this.peekOffset2Px * 1.4;
  }

  private scaleFor(offset: number): number {
    const abs = Math.abs(offset);
    if (abs === 0) return 1;
    return abs === 1 ? NEAR_SCALE : FAR_SCALE;
  }

  private opacityFor(offset: number): number {
    const abs = Math.abs(offset);
    if (abs === 0) return 1;
    if (abs === 1) return NEAR_OPACITY;
    if (abs === 2) return FAR_OPACITY;
    return HIDDEN_OPACITY;
  }

  private blurFor(offset: number): string {
    const abs = Math.abs(offset);
    if (abs === 0) return 'blur(0px)';
    return abs === 1 ? 'blur(2px)' : 'blur(4px)';
  }

  private zIndexFor(offset: number): number {
    const abs = Math.abs(offset);
    if (abs === 0) return 4;
    if (abs === 1) return 3;
    return abs === 2 ? 2 : 1;
  }

  private startAutoplay(): void {
    if (!this.isBrowser || this.autoplayTimer) return;
    this.autoplayTimer = setInterval(() => this.go(1), AUTOPLAY_INTERVAL_MS);
  }

  private stopAutoplay(): void {
    if (this.autoplayTimer) {
      clearInterval(this.autoplayTimer);
      this.autoplayTimer = null;
    }
  }

  private restartAutoplay(): void {
    this.stopAutoplay();
    this.startAutoplay();
  }
}
