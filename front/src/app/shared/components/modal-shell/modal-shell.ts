import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  effect,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { animateModalIn, animateModalOut } from '../../utils/modal-motion';

@Component({
  selector: 'app-modal-shell',
  imports: [],
  templateUrl: './modal-shell.html',
  styleUrl: './modal-shell.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '(document:keydown.escape)': 'onEsc()',
  },
})
export class ModalShell {
  /** Whether the modal is open. */
  readonly open = input.required<boolean>();
  /** Optional title rendered in the header. */
  readonly title = input<string>('');
  /** Max width of the panel (px). */
  readonly maxWidth = input<number>(480);

  readonly closed = output<void>();

  private readonly panelRef = viewChild<ElementRef<HTMLElement>>('panel');
  private readonly backdropRef = viewChild<ElementRef<HTMLElement>>('backdrop');
  private readonly closing = signal(false);

  constructor() {
    effect(() => {
      const isOpen = this.open();
      document.body.classList.toggle('no-scroll', isOpen);
      if (!isOpen) return;
      this.closing.set(false);
      queueMicrotask(() => {
        const panel = this.panelRef()?.nativeElement;
        const backdrop = this.backdropRef()?.nativeElement;
        if (panel && backdrop) animateModalIn(panel, backdrop);
      });
    });
  }

  requestClose(): void {
    if (this.closing()) return;
    const panel = this.panelRef()?.nativeElement;
    const backdrop = this.backdropRef()?.nativeElement;
    if (!panel || !backdrop) {
      this.closed.emit();
      return;
    }
    this.closing.set(true);
    animateModalOut(panel, backdrop).then(() => this.closed.emit());
  }

  protected onEsc(): void {
    if (this.open()) this.requestClose();
  }
}
