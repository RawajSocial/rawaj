import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

/**
 * Wraps a brand-dependent action affordance and, when `locked`, blurs it and
 * overlays a lock + "create a brand profile" CTA — matching the visual style of
 * `meta-analytics-card`. Used to lock create/new/generate controls for a tenant
 * that has no brand profile yet.
 */
@Component({
  selector: 'app-brand-lock',
  imports: [RouterLink],
  templateUrl: './brand-lock.html',
  styleUrl: './brand-lock.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BrandLock {
  readonly locked = input.required<boolean>();
  readonly message = input('أنشئ ملف علامة تجارية أولاً لاستخدام هذه الميزة.');
  readonly ctaLabel = input('أنشئ ملف علامة تجارية');
}
