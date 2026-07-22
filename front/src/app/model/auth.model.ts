/** Mirrors the backend's `Rawaj.Domain.Enums.Language` enum (serialized as a string). */
export type AppLanguage = 'En' | 'Ar';

/** POST /api/v1/auth/register — Rawaj.Application.Features.Auth.Register.RegisterCommand */
export interface RegisterRequest {
  email: string;
  password: string;
  fullName: string;
  preferredLanguage: AppLanguage;
}

/** Rawaj.Application.Features.Auth.Register.RegisterResponse */
export interface RegisterResponse {
  userId: string;
  email: string;
  fullName: string;
  accessToken: string;
  refreshToken: string;
}

/** POST /api/v1/auth/login — Rawaj.Application.Features.Auth.Login.LoginCommand */
export interface LoginRequest {
  email: string;
  password: string;
}

/** Rawaj.Application.Features.Auth.Login.LoginResponse */
export interface LoginResponse {
  userId: string;
  email: string;
  fullName: string;
  accessToken: string;
  refreshToken: string;
}

/** Rawaj.Application.Features.Auth.RefreshToken.RefreshTokenResponse */
export interface RefreshTokenResponse {
  accessToken: string;
  refreshToken: string;
}

/** Decoded payload of the backend's JWT access token (see JwtTokenGenerator). */
export interface AccessTokenClaims {
  sub: string;
  email: string;
  jti: string;
  full_name: string;
  preferred_language: AppLanguage;
  platform_admin: 'true' | 'false';
  exp: number;
  iss: string;
  aud: string;
}

/** The app-facing shape derived from the decoded access token. */
export interface AuthUser {
  id: string;
  email: string;
  fullName: string;
  preferredLanguage: AppLanguage;
  isPlatformAdmin: boolean;
}

/** Backend's generic `ApiResponse<T>` envelope (see Rawaj.Common.ApiResponse). */
export interface ApiResponse<T> {
  status: 'success' | 'fail' | 'error';
  data: T | null;
  message: string | null;
  /** Present on 400s raised by FluentValidation — field name -> messages. */
  errors: Record<string, string[]> | null;
}
