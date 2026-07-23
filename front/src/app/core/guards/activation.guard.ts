import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { TenantService } from '../tenant/tenant.service';

/**
 * Blocks pages that require a completed brand-owner profile — campaigns, brand profiles, billing,
 * team management, social accounts — while the user's OWN tenant (not necessarily the one they're
 * currently acting in) is still unactivated. This is a UX gate, not a security boundary: the
 * backend independently enforces tenant/brand access on every request regardless of this guard.
 */
export const activationGuard: CanActivateFn = (_route, state) => {
  const tenantService = inject(TenantService);
  const router = inject(Router);

  // Data not loaded yet (e.g. very first navigation before the app initializer resolves) — don't
  // incorrectly flash the locked page; let the route through and re-check on the next navigation.
  if (tenantService.memberships().length === 0) return true;

  if (tenantService.isOwnTenantActivated()) return true;

  return router.createUrlTree(['/dashboard/locked'], { queryParams: { returnUrl: state.url } });
};
