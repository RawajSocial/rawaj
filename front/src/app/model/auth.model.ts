/** Mirrors the backend's `Rawaj.Domain.Enums.Language` enum (serialized as a string). */
export type AppLanguage = 'En' | 'Ar';

/** POST /api/v1/auth/register — Rawaj.Application.Features.Auth.Register.RegisterCommand */
export interface RegisterRequest {
  email: string;
  username: string;
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
  identifier: string;
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
  username: string;
  jti: string;
  full_name: string;
  preferred_language: AppLanguage;
  platform_admin: 'true' | 'false';
  exp: number;
  iss: string;
  aud: string;
}

/** The app-facing shape derived from the decoded access token, optionally refined by `/users/me`. */
export interface AuthUser {
  id: string;
  email: string;
  username: string;
  fullName: string;
  preferredLanguage: AppLanguage;
  isPlatformAdmin: boolean;
  avatarUrl?: string;
  emailConfirmed?: boolean;
}

/** GET /api/v1/users/me — Rawaj.Application.Features.Users.GetMyProfile.GetMyProfileResponse */
export interface MyProfileResponse {
  userId: string;
  email: string;
  username: string;
  fullName: string;
  avatarUrl?: string;
  preferredLanguage: AppLanguage;
  emailConfirmed: boolean;
}

/** PUT /api/v1/users/me — Rawaj.Application.Features.Users.UpdateMyProfile.UpdateMyProfileCommand */
export interface UpdateMyProfileRequest {
  fullName?: string;
  username?: string;
  preferredLanguage?: AppLanguage;
}

/** POST /api/v1/users/me/change-password — Rawaj.Application.Features.Users.ChangeMyPassword.ChangeMyPasswordCommand */
export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

/** POST /api/v1/users/me/avatar — Rawaj.Application.Features.Users.UpdateMyAvatar.UpdateMyAvatarResponse */
export interface UpdateAvatarResponse {
  avatarUrl: string;
}

/** POST /api/v1/users/me/email/verify-otp — Rawaj.Application.Features.Users.VerifyEmailOtp.VerifyEmailOtpCommand */
export interface VerifyEmailOtpRequest {
  code: string;
}

/** Backend's generic `ApiResponse<T>` envelope (see Rawaj.Common.ApiResponse). */
export interface ApiResponse<T> {
  status: 'success' | 'fail' | 'error';
  data: T | null;
  message: string | null;
  /** Present on 400s raised by FluentValidation — field name -> messages. */
  errors: Record<string, string[]> | null;
}
