import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../auth/auth.service';

/**
 * Attaches the bearer token to every request targeting our own API, and on a 401 attempts exactly
 * one token refresh before retrying - never for the auth endpoints themselves (a 401 from
 * login/refresh-token means bad credentials or an invalid refresh token, not an expired access
 * token, so retrying there would either loop or mask the real error).
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const isApiRequest = req.url.startsWith(environment.apiBaseUrl);
  const isAuthEndpoint = req.url.includes('/auth/login') || req.url.includes('/auth/register') || req.url.includes('/auth/refresh-token');

  const authorizedReq =
    isApiRequest && authService.accessToken && !isAuthEndpoint
      ? req.clone({ setHeaders: { Authorization: `Bearer ${authService.accessToken}` } })
      : req;

  return next(authorizedReq).pipe(
    catchError((error: unknown) => {
      const shouldAttemptRefresh =
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        isApiRequest &&
        !isAuthEndpoint &&
        authService.refreshToken !== null;

      if (!shouldAttemptRefresh) {
        return throwError(() => error);
      }

      return authService.refreshSession().pipe(
        switchMap(() => {
          const retriedReq = req.clone({ setHeaders: { Authorization: `Bearer ${authService.accessToken}` } });
          return next(retriedReq);
        }),
        catchError((refreshError: unknown) => {
          authService.logout();
          router.navigate(['/login']);
          return throwError(() => refreshError);
        }),
      );
    }),
  );
};
