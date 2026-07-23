import { animate } from 'motion';

const EASE_OUT = [0.16, 1, 0.3, 1] as const;
const CONFETTI_COLORS = ['#7C3AED', '#2563EB', '#F59E0B', '#10B981', '#EC4899', '#3B82F6'];

/** Confined confetti burst (motion.dev-style) — particles scatter from the center of `container`
 *  and fade out. Kept within the container's bounds since the modal panel clips overflow. */
export function burstConfetti(container: HTMLElement, count = 26): void {
  container.innerHTML = '';

  for (let i = 0; i < count; i++) {
    // Created outside Angular's template, so it won't pick up the component's scoped stylesheet —
    // every visual property is set inline here instead of via a CSS class.
    const particle = document.createElement('span');
    particle.style.position = 'absolute';
    particle.style.width = '8px';
    particle.style.height = '8px';
    particle.style.borderRadius = '2px';
    particle.style.background = CONFETTI_COLORS[i % CONFETTI_COLORS.length];
    particle.style.left = '50%';
    particle.style.top = '50%';
    particle.style.willChange = 'transform, opacity';
    container.appendChild(particle);

    const angle = Math.random() * Math.PI * 2;
    const distance = 40 + Math.random() * 70;
    const x = Math.cos(angle) * distance;
    const y = Math.sin(angle) * distance * 0.7 - 20;
    const rotate = (Math.random() - 0.5) * 540;

    animate(
      particle,
      {
        transform: [
          'translate(-50%, -50%) scale(0.5) rotate(0deg)',
          `translate(calc(-50% + ${x}px), calc(-50% + ${y}px)) scale(1) rotate(${rotate}deg)`,
        ],
        opacity: [1, 1, 0],
      },
      { duration: 0.9 + Math.random() * 0.5, ease: EASE_OUT, delay: Math.random() * 0.12 },
    );
  }
}

/** Draws the success checkmark circle + check stroke in, motion.dev "celebration button" style. */
export function animateCheckmark(circle: SVGCircleElement, check: SVGPathElement): void {
  const circleLength = circle.getTotalLength();
  const checkLength = check.getTotalLength();

  circle.style.strokeDasharray = `${circleLength}`;
  circle.style.strokeDashoffset = `${circleLength}`;
  check.style.strokeDasharray = `${checkLength}`;
  check.style.strokeDashoffset = `${checkLength}`;

  animate(circle, { strokeDashoffset: [circleLength, 0] }, { duration: 0.5, ease: EASE_OUT });
  animate(check, { strokeDashoffset: [checkLength, 0] }, { duration: 0.35, delay: 0.4, ease: EASE_OUT });
}
