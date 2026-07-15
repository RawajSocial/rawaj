import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { unwrapApiResponse } from '../api/unwrap-api-response';
import { ApiResponse, AuthSession, LoginRequest, RefreshTokenResponse, RegisterRequest } from '../models';

export interface AuthUser {
  userId: string;
  email: string;
  fullName: string;
}

const ACCESS_TOKEN_KEY = 'rawaj.auth.accessToken';
const REFRESH_TOKEN_KEY = 'rawaj.auth.refreshToken';
const USER_KEY = 'rawaj.auth.user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/auth`;

  private readonly userSignal = signal<AuthUser | null>(this.readStoredUser());
  private readonly accessTokenSignal = signal<string | null>(localStorage.getItem(ACCESS_TOKEN_KEY));

  readonly currentUser = this.userSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.userSignal() !== null && this.accessTokenSignal() !== null);

  get accessToken(): string | null {
    return this.accessTokenSignal();
  }

  get refreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  }

  register(request: RegisterRequest): Observable<AuthSession> {
    return this.http
      .post<ApiResponse<AuthSession>>(`${this.baseUrl}/register`, request)
      .pipe(unwrapApiResponse(), tap((session) => this.persistSession(session)));
  }

  login(request: LoginRequest): Observable<AuthSession> {
    return this.http
      .post<ApiResponse<AuthSession>>(`${this.baseUrl}/login`, request)
      .pipe(unwrapApiResponse(), tap((session) => this.persistSession(session)));
  }

  /** Called directly by the HTTP interceptor on a 401, bypassing it to avoid recursive auth handling. */
  refreshSession(): Observable<RefreshTokenResponse> {
    return this.http
      .post<ApiResponse<RefreshTokenResponse>>(`${this.baseUrl}/refresh-token`, {
        refreshToken: this.refreshToken,
      })
      .pipe(
        unwrapApiResponse(),
        tap((tokens) => {
          this.accessTokenSignal.set(tokens.accessToken);
          localStorage.setItem(ACCESS_TOKEN_KEY, tokens.accessToken);
          localStorage.setItem(REFRESH_TOKEN_KEY, tokens.refreshToken);
        }),
      );
  }

  logout(): void {
    const refreshToken = this.refreshToken;
    this.clearSession();

    if (refreshToken) {
      // Fire-and-forget: the session is already cleared locally regardless of whether the
      // server-side revocation call succeeds.
      this.http.post(`${this.baseUrl}/logout`, { refreshToken }).subscribe({ error: () => undefined });
    }
  }

  private persistSession(session: AuthSession): void {
    const user: AuthUser = { userId: session.userId, email: session.email, fullName: session.fullName };

    this.userSignal.set(user);
    this.accessTokenSignal.set(session.accessToken);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    localStorage.setItem(ACCESS_TOKEN_KEY, session.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, session.refreshToken);
  }

  private clearSession(): void {
    this.userSignal.set(null);
    this.accessTokenSignal.set(null);
    localStorage.removeItem(USER_KEY);
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
  }

  private readStoredUser(): AuthUser | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) {
      return null;
    }

    try {
      return JSON.parse(raw) as AuthUser;
    } catch {
      return null;
    }
  }
}
