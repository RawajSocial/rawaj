import { ScheduledPostStatus, SocialPlatform } from '../core/models';

export type PostStatus = ScheduledPostStatus;

export interface ScheduledPost {
  id: string;
  contentItemId: string;
  platform: SocialPlatform;
  accountName: string;
  scheduledAt: string; // ISO
  status: PostStatus;
  publishedAt: string | null;
  errorMessage: string | null;
  // Populated lazily (single fetch) only when the post's detail modal is opened - the list
  // endpoint doesn't return caption/hashtags to avoid an N+1 content-item fetch per calendar cell.
  content?: string;
  hashtags?: string[];
}
