import { Directive, input } from '@angular/core';

export type TooltipPosition = 'top' | 'bottom' | 'start' | 'end';

/**
 * Lightweight, CSS-only tooltip for icon-only controls (row actions, buttons
 * with no visible label). Renders via `::after`/`::before` in tooltip.css
 * using `attr(data-tooltip)`, so it needs no overlay/positioning service.
 */
@Directive({
  selector: '[appTooltip]',
  host: {
    class: 'rw-tooltip-host',
    '[class]': "'rw-tooltip-host--' + position()",
    '[attr.data-tooltip]': 'appTooltip()',
  },
})
export class TooltipDirective {
  readonly appTooltip = input.required<string>();
  readonly position = input<TooltipPosition>('top');
}
