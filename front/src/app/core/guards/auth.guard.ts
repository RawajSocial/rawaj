import { inject } from '@angular/core';
import { CanActivateFn, CanMatchFn, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';

/** Blocks activating a route unless the user has a valid (non-expired) access token. */
export const authGuard: CanActivateFn = (_route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) return true;

  return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

/** Same check as `authGuard`, but for lazy-loaded route matching (keeps the chunk from loading at all). */
export const authCanMatch: CanMatchFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) return true;

  return router.createUrlTree(['/login']);
};
