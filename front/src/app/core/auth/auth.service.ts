import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient, HttpEventType, HttpResponse } from '@angular/common/http';
import { filter, finalize, map, Observable, shareReplay, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AccessTokenClaims, ApiResponse, AuthUser, ChangePasswordRequest, LoginRequest, LoginResponse,
  MyProfileResponse, RefreshTokenResponse, RegisterRequest, RegisterResponse, UpdateAvatarResponse,
  UpdateMyProfileRequest,
} from '../../model/auth.model';
import { decodeAccessToken, isTokenExpired } from './jwt.util';
import { resolveMediaUrl } from './media-url.util';

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

  /** Supplements (never replaces) the JWT-derived baseline with data fetched from `/users/me`. */
  private readonly _profileOverride = signal<Partial<AuthUser> | null>(null);

  readonly currentUser = computed<AuthUser | null>(() => {
    const c = this.claims();
    if (!c) return null;
    return {
      id: c.sub,
      email: c.email,
      username: c.username,
      fullName: c.full_name,
      preferredLanguage: c.preferred_language,
      isPlatformAdmin: c.platform_admin === 'true',
      ...this._profileOverride(),
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

  private refreshInFlight$: Observable<string | null> | null = null;

  /**
   * Refresh tokens rotate (single-use) on the backend, so concurrent 401s must share one
   * in-flight refresh instead of each calling refreshAccessToken() with an already-rotated token.
   */
  refreshAccessTokenShared(): Observable<string | null> {
    if (!this.refreshInFlight$) {
      this.refreshInFlight$ = this.refreshAccessToken().pipe(
        map(res => res.data?.accessToken ?? null),
        finalize(() => {
          this.refreshInFlight$ = null;
        }),
        shareReplay(1),
      );
    }
    return this.refreshInFlight$;
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
    this._profileOverride.set(null);
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
  }

  fetchMyProfile(): Observable<ApiResponse<MyProfileResponse>> {
    return this.http.get<ApiResponse<MyProfileResponse>>(`${environment.apiUrl}/users/me`).pipe(
      tap(res => {
        if (res.data) this._profileOverride.set({ ...res.data, avatarUrl: resolveMediaUrl(res.data.avatarUrl) });
      }),
    );
  }

  updateMyProfile(request: UpdateMyProfileRequest): Observable<ApiResponse<MyProfileResponse>> {
    return this.http.put<ApiResponse<MyProfileResponse>>(`${environment.apiUrl}/users/me`, request).pipe(
      tap(res => {
        if (res.data) this._profileOverride.set({ ...res.data, avatarUrl: resolveMediaUrl(res.data.avatarUrl) });
      }),
    );
  }

  changePassword(request: ChangePasswordRequest): Observable<ApiResponse<boolean>> {
    return this.http.post<ApiResponse<boolean>>(`${environment.apiUrl}/users/me/change-password`, request);
  }

  /**
   * `onProgress` is called with 0-100 as the upload streams; unsubscribing the returned
   * Observable aborts the underlying request, which is how upload cancellation is implemented.
   */
  uploadAvatar(file: File, onProgress?: (percent: number) => void): Observable<ApiResponse<UpdateAvatarResponse>> {
    const formData = new FormData();
    formData.append('avatar', file);
    return this.http
      .post<ApiResponse<UpdateAvatarResponse>>(`${environment.apiUrl}/users/me/avatar`, formData, {
        reportProgress: true,
        observe: 'events',
      })
      .pipe(
        tap(event => {
          if (event.type === HttpEventType.UploadProgress && event.total) {
            onProgress?.(Math.round((100 * event.loaded) / event.total));
          }
        }),
        filter(
          (event): event is HttpResponse<ApiResponse<UpdateAvatarResponse>> => event.type === HttpEventType.Response,
        ),
        map(event => event.body!),
        tap(res => {
          if (res.data) this._profileOverride.update(o => ({ ...o, avatarUrl: resolveMediaUrl(res.data!.avatarUrl) }));
        }),
      );
  }

  deleteAvatar(): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${environment.apiUrl}/users/me/avatar`).pipe(
      tap(res => {
        if (res.data) this._profileOverride.update(o => ({ ...o, avatarUrl: undefined }));
      }),
    );
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
