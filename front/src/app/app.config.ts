import { ApplicationConfig, inject, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, Router } from '@angular/router';
import { HttpErrorResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { catchError, firstValueFrom, of } from 'rxjs';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { AuthService } from './core/auth/auth.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAppInitializer(() => {
      const authService = inject(AuthService);
      const router = inject(Router);

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
      );
    }),
  ]
};
