export type SocialPlatform = 'Instagram' | 'Linkedin' | 'Twitter' | 'Facebook';

export interface SocialAccountSummary {
  socialAccountId: string;
  platform: SocialPlatform;
  accountName: string;
  isActive: boolean;
  tokenExpiresAt: string | null;
  lastVerifiedAt: string | null;
}

export interface GetAuthorizationUrlResponse {
  authorizationUrl: string;
}

export interface DisconnectSocialAccountResponse {
  socialAccountId: string;
}
