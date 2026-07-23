import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { TenantService } from '../tenant/tenant.service';

/**
 * Attaches `X-Tenant-Id` to every outgoing API request so the backend knows which of the user's
 * tenants (their own, or one they've been invited into) the request should act in — see
 * `TenantResolutionPolicy` on the backend. Omitted entirely when no tenant is active yet (e.g.
 * during registration), letting the backend fall back to its own default.
 */
export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
  const tenantService = inject(TenantService);
  const activeTenantId = tenantService.activeTenantId();

  const tenantReq = activeTenantId
    ? req.clone({ setHeaders: { 'X-Tenant-Id': activeTenantId } })
    : req;

  return next(tenantReq);
};
