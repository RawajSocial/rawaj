import { inject } from '@angular/core';
import { CanActivateFn, CanMatchFn, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';

/** Blocks `/admin` for anyone who isn't logged in, or who's logged in but not a platform admin. */
export const adminGuard: CanActivateFn = (_route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
  }
  if (!authService.isPlatformAdmin()) {
    return router.createUrlTree(['/dashboard']);
  }
  return true;
};

/** Same check as `adminGuard`, but for lazy-loaded route matching. */
export const adminCanMatch: CanMatchFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return router.createUrlTree(['/login']);
  }
  if (!authService.isPlatformAdmin()) {
    return router.createUrlTree(['/dashboard']);
  }
  return true;
};
