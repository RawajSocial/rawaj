import { ChangeDetectionStrategy, Component, effect, input, signal } from '@angular/core';

export type AvatarSize = 'sm' | 'md' | 'lg';

/**
 * Single source of truth for rendering a user avatar: fixed-size circular
 * container, image constrained via CSS only (never sized by the uploaded
 * image), and automatic fallback to a default icon on a broken/missing URL.
 */
@Component({
  selector: 'app-avatar',
  imports: [],
  templateUrl: './avatar.html',
  styleUrl: './avatar.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Avatar {
  readonly src = input<string | null | undefined>(null);
  readonly alt = input('');
  readonly size = input<AvatarSize>('md');

  protected readonly loadFailed = signal(false);

  constructor() {
    effect(() => {
      this.src();
      this.loadFailed.set(false);
    });
  }

  protected onError(): void {
    this.loadFailed.set(true);
  }
}
