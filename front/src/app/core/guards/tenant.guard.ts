import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { TenantService } from '../tenant/tenant.service';

/** Keeps a user with no tenant yet out of the dashboard - they land on /welcome instead. */
export const tenantGuard: CanActivateFn = () => {
  const tenantService = inject(TenantService);
  const router = inject(Router);

  if (tenantService.loaded()) {
    return tenantService.hasTenant() || router.createUrlTree(['/welcome']);
  }

  return tenantService.loadContext().pipe(
    map(() => tenantService.hasTenant() || router.createUrlTree(['/welcome'])),
  );
};

/** Sends a user who already has a tenant straight to their dashboard instead of /welcome. */
export const noTenantGuard: CanActivateFn = () => {
  const tenantService = inject(TenantService);
  const router = inject(Router);

  if (tenantService.loaded()) {
    return !tenantService.hasTenant() || router.createUrlTree(['/dashboard']);
  }

  return tenantService.loadContext().pipe(
    map(() => !tenantService.hasTenant() || router.createUrlTree(['/dashboard'])),
  );
};
