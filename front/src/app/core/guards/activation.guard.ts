import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
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

/**
 * Blocks pages that need an actual brand profile to be useful — campaigns, calendar, marketing
 * plan, billing, team management, social accounts. Requires BOTH the business-info activation
 * (see `activationGuard`) AND at least one brand profile on the active tenant. Does not apply to
 * `brand-profiles`/`brand-profiles/new` themselves, since those are where a missing brand profile
 * gets fixed — gating them on brand-profile existence would make them unreachable.
 */
export const brandAccessGuard: CanActivateFn = (_route, state) => {
  const tenantService = inject(TenantService);
  const router = inject(Router);

  if (tenantService.memberships().length === 0) return true;

  const evaluate = () =>
    tenantService.isOwnTenantActivated() && tenantService.brandProfileCount() > 0
      ? true
      : router.createUrlTree(['/dashboard/locked'], { queryParams: { returnUrl: state.url } });

  // `brandProfileCount` comes from the active-tenant summary (`tenant()`), which is only fetched
  // once the dashboard layout component constructs — but router guards for its child routes run
  // *before* that component is instantiated. On a fresh navigation straight into a
  // brandAccessGuard route (direct link, page refresh, post-login redirect) `tenant()` can still
  // be null here, which would make `brandProfileCount()` default to 0 and wrongly lock a user who
  // already has a brand profile. Fetch it directly instead of assuming zero.
  if (!tenantService.tenant()) {
    return tenantService.refresh().pipe(
      map(() => evaluate()),
      catchError(() => of(true)),
    );
  }

  return evaluate();
};
