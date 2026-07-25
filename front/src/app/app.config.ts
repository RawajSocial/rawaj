import { ApplicationConfig, inject, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, Router } from '@angular/router';
import { HttpErrorResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { catchError, firstValueFrom, of } from 'rxjs';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { tenantInterceptor } from './core/interceptors/tenant.interceptor';
import { AuthService } from './core/auth/auth.service';
import { TenantService } from './core/tenant/tenant.service';
import { FeatureFlagsService } from './services/feature-flags.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor, tenantInterceptor])),
    provideAppInitializer(() => {
      const authService = inject(AuthService);
      const tenantService = inject(TenantService);
      const router = inject(Router);
      const featureFlagsService = inject(FeatureFlagsService);

      featureFlagsService.refresh();

      if (!authService.accessToken()) return Promise.resolve();

      return firstValueFrom(
        authService.fetchMyProfile().pipe(
          catchError((err: HttpErrorResponse) => {
            if (err.status === 401 || err.status === 403) {
              authService.clearSession();
              router.navigate(['/login']);
            }
            // Network/server errors: swallow and let the app boot with JWT-derived state;
            // the auth interceptor's existing refresh-on-401 flow covers it from here.
            return of(null);
          }),
        ),
      ).then(() => {
        if (!authService.isAuthenticated()) return;
        // Populates memberships (and picks/keeps an active tenant) before any tenant-scoped
        // request fires, so the X-Tenant-Id header is correct from the very first navigation.
        return firstValueFrom(tenantService.refreshMemberships().pipe(catchError(() => of(null))));
      });
    }),
  ]
};
