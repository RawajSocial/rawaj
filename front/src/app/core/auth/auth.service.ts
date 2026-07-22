import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AccessTokenClaims, ApiResponse, AuthUser, LoginRequest, LoginResponse,
  RefreshTokenResponse, RegisterRequest, RegisterResponse,
} from '../../model/auth.model';
import { decodeAccessToken, isTokenExpired } from './jwt.util';

const ACCESS_TOKEN_KEY = 'rawaj_access_token';
const REFRESH_TOKEN_KEY = 'rawaj_refresh_token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/auth`;

  private readonly _accessToken = signal<string | null>(localStorage.getItem(ACCESS_TOKEN_KEY));
  private readonly _refreshToken = signal<string | null>(localStorage.getItem(REFRESH_TOKEN_KEY));

  /** Decoded claims of the current access token, or null if absent/malformed. */
  private readonly claims = computed<AccessTokenClaims | null>(() => {
    const token = this._accessToken();
    return token ? decodeAccessToken(token) : null;
  });

  readonly accessToken = this._accessToken.asReadonly();

  readonly currentUser = computed<AuthUser | null>(() => {
    const c = this.claims();
    if (!c) return null;
    return {
      id: c.sub,
      email: c.email,
      fullName: c.full_name,
      preferredLanguage: c.preferred_language,
      isPlatformAdmin: c.platform_admin === 'true',
    };
  });

  readonly isAuthenticated = computed(() => {
    const c = this.claims();
    return !!c && !isTokenExpired(c);
  });

  readonly isPlatformAdmin = computed(() => this.currentUser()?.isPlatformAdmin ?? false);

  register(request: RegisterRequest): Observable<ApiResponse<RegisterResponse>> {
    return this.http.post<ApiResponse<RegisterResponse>>(`${this.baseUrl}/register`, request).pipe(
      tap(res => {
        if (res.data) this.storeSession(res.data.accessToken, res.data.refreshToken);
      }),
    );
  }

  login(request: LoginRequest): Observable<ApiResponse<LoginResponse>> {
    return this.http.post<ApiResponse<LoginResponse>>(`${this.baseUrl}/login`, request).pipe(
      tap(res => {
        if (res.data) this.storeSession(res.data.accessToken, res.data.refreshToken);
      }),
    );
  }

  refreshAccessToken(): Observable<ApiResponse<RefreshTokenResponse>> {
    return this.http
      .post<ApiResponse<RefreshTokenResponse>>(`${this.baseUrl}/refresh-token`, {
        refreshToken: this._refreshToken(),
      })
      .pipe(
        tap(res => {
          if (res.data) this.storeSession(res.data.accessToken, res.data.refreshToken);
        }),
      );
  }

  logout(): Observable<ApiResponse<boolean>> {
    const refreshToken = this._refreshToken();
    this.clearSession();
    return this.http.post<ApiResponse<boolean>>(`${this.baseUrl}/logout`, { refreshToken });
  }

  /** Synchronous cleanup for cases (e.g. an unrecoverable 401) where no logout request is needed. */
  clearSession(): void {
    this._accessToken.set(null);
    this._refreshToken.set(null);
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return this._refreshToken();
  }

  private storeSession(accessToken: string, refreshToken: string): void {
    this._accessToken.set(accessToken);
    this._refreshToken.set(refreshToken);
    localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
  }
}
