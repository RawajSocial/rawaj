import { ScheduledPostStatus, SocialPlatform } from './enums';

export interface ScheduledPostSummary {
  scheduledPostId: string;
  contentItemId: string;
  platform: SocialPlatform;
  accountName: string;
  scheduledAt: string;
  status: ScheduledPostStatus;
  publishedAt: string | null;
  errorMessage: string | null;
}

export interface SchedulePostRequest {
  contentItemId: string;
  visualAssetId?: string | null;
  socialAccountId: string;
  scheduledAt: string;
  aiSuggestedTime: boolean;
}

export interface SchedulePostResponse {
  scheduledPostId: string;
  scheduledAt: string;
  status: ScheduledPostStatus;
  externalPostId: string | null;
}

export interface CancelScheduledPostResponse {
  scheduledPostId: string;
  status: ScheduledPostStatus;
}

export interface PublishScheduledPostResponse {
  scheduledPostId: string;
  status: ScheduledPostStatus;
  externalPostId: string | null;
}
