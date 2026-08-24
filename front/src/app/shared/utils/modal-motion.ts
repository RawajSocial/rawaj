import { animate } from 'motion';

const EASE_OUT = [0.16, 1, 0.3, 1] as const;

export function animateModalIn(panel: Element, backdrop: Element): void {
  animate(backdrop, { opacity: [0, 1] }, { duration: 0.2 });
  animate(
    panel,
    { opacity: [0, 1], transform: ['translateY(16px) scale(0.96)', 'translateY(0) scale(1)'] },
    { duration: 0.28, ease: EASE_OUT },
  );
}

export function animateModalOut(panel: Element, backdrop: Element): Promise<void> {
  animate(backdrop, { opacity: [1, 0] }, { duration: 0.18 });
  return animate(
    panel,
    { opacity: [1, 0], transform: ['translateY(0) scale(1)', 'translateY(10px) scale(0.97)'] },
    { duration: 0.18, ease: EASE_OUT },
  ).finished.then(() => undefined);
}
