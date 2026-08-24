import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { TenantService } from '../tenant/tenant.service';

/**
 * Blocks pages that require a completed brand-owner profile — campaigns, brand profiles, billing,
 * team management, social accounts. For the tenant OWNER this means their own tenant (not
 * necessarily the one currently active) must be activated. For an INVITED (non-owner) member,
 * "their own tenant" is irrelevant to whether the tenant that invited them is usable — they're
 * gated on their own lightweight account setup instead (see TenantService.accountSetupCompleted),
 * which unlocks the pages this guard does NOT protect (content-gen, ads) regardless of whether the
 * active tenant itself is activated; brandAccessGuard-protected pages below still separately
 * require the ACTIVE tenant's own activation, which only its owner can provide.
 * This is a UX gate, not a security boundary: the backend independently enforces tenant/brand
 * access on every request regardless of this guard.
 */
export const activationGuard: CanActivateFn = (_route, state) => {
  const tenantService = inject(TenantService);
  const router = inject(Router);

  // Data not loaded yet (e.g. very first navigation before the app initializer resolves) — don't
  // incorrectly flash the locked page; let the route through and re-check on the next navigation.
  if (tenantService.memberships().length === 0) return true;

  const unlocked = tenantService.isActiveMemberOwner()
    ? tenantService.isOwnTenantActivated()
    : tenantService.accountSetupCompleted();
  if (unlocked) return true;

  const reason = tenantService.isActiveMemberOwner() ? 'activation' : 'account-setup';
  return router.createUrlTree(['/dashboard/locked'], { queryParams: { returnUrl: state.url, reason } });
};

/**
 * Blocks pages that need an actual brand profile to be useful — campaigns, calendar, marketing
 * plan, billing, team management, social accounts. Requires at least one brand profile AND the
 * relevant tenant's activation — for the OWNER that's their own tenant (unchanged from before);
 * for an INVITED member it's the ACTIVE tenant's own activation (the tenant that invited them),
 * since campaign/plan data belongs to that tenant regardless of the member's personal setup state.
 * Does not apply to `brand-profiles`/`brand-profiles/new` themselves, since those are where a
 * missing brand profile gets fixed — gating them on brand-profile existence would make them
 * unreachable.
 */
export const brandAccessGuard: CanActivateFn = (_route, state) => {
  const tenantService = inject(TenantService);
  const router = inject(Router);

  if (tenantService.memberships().length === 0) return true;

  const evaluate = () => {
    const activated = tenantService.isActiveMemberOwner()
      ? tenantService.isOwnTenantActivated()
      : tenantService.isActivated();
    return activated && tenantService.brandProfileCount() > 0
      ? true
      : router.createUrlTree(['/dashboard/locked'], {
          queryParams: { returnUrl: state.url, reason: activated ? undefined : 'activation' },
        });
  };

  // `brandProfileCount`/`isActivated` come from the active-tenant summary (`tenant()`), which is
  // only fetched once the dashboard layout component constructs — but router guards for its child
  // routes run *before* that component is instantiated. On a fresh navigation straight into a
  // brandAccessGuard route (direct link, page refresh, post-login redirect) `tenant()` can still
  // be null here, which would make these checks wrongly default to a locked state. Fetch it
  // directly instead of assuming the worst.
  if (!tenantService.tenant()) {
    return tenantService.refresh().pipe(
      map(() => evaluate()),
      catchError(() => of(true)),
    );
  }

  return evaluate();
};
