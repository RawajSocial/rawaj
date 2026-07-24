import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { TenantService } from '../tenant/tenant.service';

/** The exact backend message for "the requested X-Tenant-Id isn't a tenant the caller is an
 *  accepted member of" (see TenantResolutionPolicy.ResolveAsync) — matched precisely so this only
 *  self-heals that specific case, never a legitimate role-permission 403. */
const STALE_TENANT_MESSAGE = 'You do not belong to this organization.';

/**
 * Attaches `X-Tenant-Id` to every outgoing API request so the backend knows which of the user's
 * tenants (their own, or one they've been invited into) the request should act in — see
 * `TenantResolutionPolicy` on the backend. Omitted entirely when no tenant is active yet (e.g.
 * during registration), letting the backend fall back to its own default.
 *
 * The cached active tenant can go stale (removed from that org, a leftover id from another
 * session on the same browser, etc.) and the backend correctly rejects it with a 403 rather than
 * silently falling back — when that happens here, self-heal by refreshing memberships (which picks
 * a tenant the user actually belongs to) and retrying exactly once, so a stale id never surfaces
 * as a user-facing error.
 */
export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
  const tenantService = inject(TenantService);
  const activeTenantId = tenantService.activeTenantId();

  const tenantReq = activeTenantId
    ? req.clone({ setHeaders: { 'X-Tenant-Id': activeTenantId } })
    : req;

  return next(tenantReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 403 || !activeTenantId || error.error?.message !== STALE_TENANT_MESSAGE) {
        return throwError(() => error);
      }

      return tenantService.refreshMemberships().pipe(
        switchMap(() => {
          const healedTenantId = tenantService.activeTenantId();
          if (!healedTenantId || healedTenantId === activeTenantId) {
            return throwError(() => error);
          }
          const retryReq = req.clone({ setHeaders: { 'X-Tenant-Id': healedTenantId } });
          return next(retryReq);
        }),
        catchError(() => throwError(() => error)),
      );
    }),
  );
};
