import { CampaignPlatform } from './campaign.model';
import { BackendSocialPlatform } from './content-item.model';

export type PostStatus = 'scheduled' | 'published' | 'failed' | 'draft' | 'taken-down';
export type MediaType  = 'image' | 'video' | 'carousel' | 'reel' | 'story';

export interface ScheduledPost {
  id: string;
  contentItemId: string;
  campaignId: string;
  campaignName: string;
  platform: CampaignPlatform;
  content: string;
  mediaType?: MediaType;
  imageUrl?: string;
  scheduledAt: string; // true UTC instant, ISO with offset: "2026-06-04T08:30:00Z" — always format/edit
                       // via shared/utils/cairo-time.util.ts, never with the browser's own timezone.
  status: PostStatus;
  hashtags?: string[];
  estimatedReach?: number;
  /** The platform's own id for the live post, set once actually published — use with
   *  shared/utils/social-links.util.ts to build a link to the real post, don't build the URL
   *  inline (Instagram/Facebook permalinks aren't just "domain + id"). */
  postId?: string;
}

/** GET /api/v1/scheduled-posts — Rawaj.Application.Features.Scheduling.GetScheduledPosts.ScheduledPostSummary */
export interface ScheduledPostSummary {
  scheduledPostId: string;
  contentItemId: string;
  brandProfileId: string;
  campaignId?: string | null;
  platform: BackendSocialPlatform;
  accountName: string;
  scheduledAt: string;
  status: 'Pending' | 'Published' | 'Failed' | 'Cancelled' | 'TakenDown';
  publishedAt?: string | null;
  errorMessage?: string | null;
  views?: number | null;
  uniqueViewers?: number | null;
  likes?: number | null;
  comments?: number | null;
  shares?: number | null;
  clicks?: number | null;
  engagementRate?: number | null;
  /** The real post copy, joined from ContentItem. */
  content: string;
  /** The specific visual asset attached at scheduling time, if any. */
  imageUrl?: string | null;
  /** The platform's own id for the live post, set once actually published. */
  postId?: string | null;
}

/** POST /api/v1/scheduled-posts/{id}/cancel — CancelScheduledPostResponse */
export interface CancelScheduledPostResponse {
  scheduledPostId: string;
  status: ScheduledPostSummary['status'];
}

/** PUT /api/v1/scheduled-posts/{id} request body — time only. Content lives on the ContentItem
 *  and is changed via regenerate, not here. */
export interface RescheduleScheduledPostRequest {
  scheduledAt: string;
}

/** PUT /api/v1/scheduled-posts/{id} — RescheduleScheduledPostResponse */
export interface RescheduleScheduledPostResponse {
  scheduledPostId: string;
  scheduledAt: string;
  status: ScheduledPostSummary['status'];
}

/** POST /api/v1/scheduled-posts/{id}/take-down — TakeDownScheduledPostResponse */
export interface TakeDownScheduledPostResponse {
  scheduledPostId: string;
  status: ScheduledPostSummary['status'];
}

/** POST /api/v1/scheduled-posts/{id}/publish-now — PublishScheduledPostResponse */
export interface PublishScheduledPostResponse {
  scheduledPostId: string;
  status: ScheduledPostSummary['status'];
  externalPostId?: string | null;
}

/** POST /api/v1/scheduled-posts request body — SchedulePostCommand. */
export interface SchedulePostRequest {
  contentItemId: string;
  visualAssetId?: string | null;
  socialAccountId: string;
  scheduledAt: string;
  aiSuggestedTime?: boolean;
}

/** POST /api/v1/scheduled-posts — SchedulePostResponse */
export interface SchedulePostResponse {
  scheduledPostId: string;
  scheduledAt: string;
  status: ScheduledPostSummary['status'];
  externalPostId?: string | null;
}

/** GET /api/v1/scheduled-posts/posting-time-suggestions — PostingTimeSuggestionDto */
export interface PostingTimeSuggestionDto {
  platform: BackendSocialPlatform;
  dayOfWeek: number; // 0=Sunday .. 6=Saturday, Cairo-local (not the viewing browser's own timezone)
  hour: number; // Cairo-local (0-23)
  fromHistoricalData: boolean;
}
