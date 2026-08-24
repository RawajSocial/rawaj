import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';

const AUTH_ENDPOINTS = ['/auth/login', '/auth/register', '/auth/refresh-token'];

/**
 * Attaches the access token to every outgoing API request, and — on a 401 —
 * tries exactly one silent refresh-and-retry before giving up and sending
 * the user back to /login.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const isAuthEndpoint = AUTH_ENDPOINTS.some(path => req.url.includes(path));
  const token = authService.accessToken();

  const authReq = token && !isAuthEndpoint
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401 || isAuthEndpoint || !authService.getRefreshToken()) {
        return throwError(() => error);
      }

      return authService.refreshAccessTokenShared().pipe(
        switchMap(newToken => {
          if (!newToken) {
            return throwError(() => error);
          }
          const retryReq = req.clone({ setHeaders: { Authorization: `Bearer ${newToken}` } });
          return next(retryReq);
        }),
        catchError(() => {
          authService.clearSession();
          router.navigate(['/login']);
          return throwError(() => error);
        }),
      );
    }),
  );
};
