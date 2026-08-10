import {
  ChangeDetectionStrategy, Component, ElementRef, afterNextRender, effect, inject, Injector, viewChild,
} from '@angular/core';
import { ModalShell } from '../modal-shell/modal-shell';
import { CelebrationModalService } from '../../../services/celebration-modal.service';
import { animateCheckmark, burstConfetti } from '../../utils/celebration-motion';

@Component({
  selector: 'app-celebration-modal',
  imports: [ModalShell],
  templateUrl: './celebration-modal.html',
  styleUrl: './celebration-modal.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CelebrationModal {
  private readonly celebrationModalService = inject(CelebrationModalService);
  private readonly injector = inject(Injector);
  protected readonly state = this.celebrationModalService.state;

  private readonly confettiContainer = viewChild<ElementRef<HTMLElement>>('confetti');
  private readonly checkCircle = viewChild<ElementRef<SVGCircleElement>>('checkCircle');
  private readonly checkPath = viewChild<ElementRef<SVGPathElement>>('checkPath');

  private hasAnimatedThisOpen = false;

  constructor() {
    effect(() => {
      const isOpen = this.state().open;
      if (!isOpen) {
        this.hasAnimatedThisOpen = false;
        return;
      }

      // ModalShell only inserts our projected content into the DOM once its own `@if(open())`
      // renders, which happens on a later change-detection pass than this effect's first run —
      // so the very first pass here always sees `undefined` refs. Reading the viewChild signals
      // directly in the effect body (not inside a setTimeout/microtask) registers them as
      // dependencies, so the effect automatically re-runs once Angular actually mounts them.
      const container = this.confettiContainer()?.nativeElement;
      const circle = this.checkCircle()?.nativeElement;
      const check = this.checkPath()?.nativeElement;
      if (!container || !circle || !check || this.hasAnimatedThisOpen) return;

      this.hasAnimatedThisOpen = true;
      // The refs being non-null only means Angular inserted the nodes — the browser hasn't
      // necessarily laid them out yet, and `getTotalLength()` throws on an SVG element that
      // hasn't been rendered. `afterNextRender` waits for that render to actually happen.
      afterNextRender(
        () => {
          burstConfetti(container);
          animateCheckmark(circle, check);
        },
        { injector: this.injector },
      );
    });
  }

  protected close(): void {
    this.celebrationModalService.close();
  }
}
