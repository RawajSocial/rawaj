import { Language } from './enums';

export interface MyProfile {
  userId: string;
  email: string;
  fullName: string;
  avatarUrl: string | null;
  preferredLanguage: Language;
}

export interface UpdateMyProfileRequest {
  fullName: string;
  preferredLanguage: Language;
  avatarUrl?: string | null;
}

export interface UpdateMyProfileResponse {
  fullName: string;
  avatarUrl: string | null;
  preferredLanguage: Language;
}

export interface ChangeMyPasswordRequest {
  currentPassword: string;
  newPassword: string;
}
