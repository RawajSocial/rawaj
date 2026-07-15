import { Language } from './enums';

export interface AuthSession {
  userId: string;
  email: string;
  fullName: string;
  accessToken: string;
  refreshToken: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  fullName: string;
  preferredLanguage: Language;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RefreshTokenResponse {
  accessToken: string;
  refreshToken: string;
}
