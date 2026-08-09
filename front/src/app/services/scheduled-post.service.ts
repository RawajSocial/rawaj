import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../model/auth.model';
import { PagedResult } from '../model/paged-result.model';
import { CampaignPlatform } from '../model/campaign.model';
import { BackendSocialPlatform } from '../model/content-item.model';
import {
  CancelScheduledPostResponse, PostingTimeSuggestionDto, PublishScheduledPostResponse,
  RescheduleScheduledPostResponse, SchedulePostRequest, SchedulePostResponse,
  ScheduledPost, ScheduledPostSummary,
} from '../model/scheduled-post.model';
import { CampaignService } from './campaign.service';

const PLATFORM_MAP: Record<BackendSocialPlatform, CampaignPlatform> = {
  Instagram: 'instagram',
  Facebook: 'facebook',
};

const STATUS_MAP: Record<ScheduledPostSummary['status'], ScheduledPost['status']> = {
  Pending: 'scheduled',
  Published: 'published',
  Failed: 'failed',
  Cancelled: 'draft',
};

@Injectable({ providedIn: 'root' })
export class ScheduledPostService {
  private readonly http = inject(HttpClient);
  private readonly campaignService = inject(CampaignService);
  private readonly baseUrl = `${environment.apiUrl}/scheduled-posts`;

  private readonly _posts = signal<ScheduledPost[]>([]);
  readonly posts = this._posts.asReadonly();

  /** Maps the backend summary (no content/campaign-name fields) into the page's display model,
   *  resolving the campaign name locally from the already-loaded CampaignService list. */
  private toScheduledPost(s: ScheduledPostSummary): ScheduledPost {
    const campaign = s.campaignId ? this.campaignService.getById(s.campaignId)() : undefined;
    return {
      id: s.scheduledPostId,
      contentItemId: s.contentItemId,
      campaignId: s.campaignId ?? '',
      campaignName: campaign?.name ?? 'بدون حملة',
      platform: PLATFORM_MAP[s.platform] ?? 'instagram',
      content: s.content,
      imageUrl: s.imageUrl ?? undefined,
      scheduledAt: s.scheduledAt,
      status: STATUS_MAP[s.status] ?? 'scheduled',
      estimatedReach: s.uniqueViewers ?? undefined,
    };
  }

  getById(id: string) {
    return computed(() => this._posts().find(p => p.id === id));
  }

  byCampaign(campaignId: string) {
    return computed(() => this._posts().filter(p => p.campaignId === campaignId));
  }

  refresh(
    brandProfileId: string,
    campaignId?: string | 'all',
    page = 1,
    pageSize = 200,
  ): Observable<ApiResponse<PagedResult<ScheduledPostSummary>>> {
    let url = `${this.baseUrl}?brandProfileId=${encodeURIComponent(brandProfileId)}&page=${page}&pageSize=${pageSize}`;
    if (campaignId && campaignId !== 'all') url += `&campaignId=${encodeURIComponent(campaignId)}`;
    return this.http.get<ApiResponse<PagedResult<ScheduledPostSummary>>>(url).pipe(
      tap(res => {
        if (res.data) this._posts.set(res.data.items.map(s => this.toScheduledPost(s)));
      }),
    );
  }

  /** Moves a pending post to a new time (time only — content lives on the ContentItem and is
   *  edited via regenerate). Patches the local list on success. */
  reschedule(id: string, scheduledAtIso: string): Observable<ApiResponse<RescheduleScheduledPostResponse>> {
    return this.http.put<ApiResponse<RescheduleScheduledPostResponse>>(
      `${this.baseUrl}/${id}`, { scheduledAt: scheduledAtIso },
    ).pipe(
      tap(res => {
        if (!res.data) return;
        const { scheduledPostId, scheduledAt } = res.data;
        this._posts.update(list => list.map(p => (p.id === scheduledPostId ? { ...p, scheduledAt, status: 'scheduled' } : p)));
      }),
    );
  }

  /** Publishes a pending post immediately, bypassing its scheduled time. */
  publishNow(id: string): Observable<ApiResponse<PublishScheduledPostResponse>> {
    return this.http.post<ApiResponse<PublishScheduledPostResponse>>(`${this.baseUrl}/${id}/publish-now`, {}).pipe(
      tap(res => {
        if (!res.data) return;
        this._posts.update(list => list.map(p => (p.id === res.data!.scheduledPostId ? { ...p, status: 'published' } : p)));
      }),
    );
  }

  /** Cancels a pending scheduled post server-side. Callers should remove it from the local
   *  list (via `remove`) once this succeeds, rather than assuming it optimistically. */
  cancel(id: string): Observable<ApiResponse<CancelScheduledPostResponse>> {
    return this.http.post<ApiResponse<CancelScheduledPostResponse>>(`${this.baseUrl}/${id}/cancel`, {});
  }

  /** Schedules a single approved content item to one connected social account — used by the
   *  standalone content-gen page's inline schedule panel. Requires a real ContentItem id, so it
   *  only applies to text-type generations there (image-only generations have no ContentItem). */
  schedule(request: SchedulePostRequest): Observable<ApiResponse<SchedulePostResponse>> {
    return this.http.post<ApiResponse<SchedulePostResponse>>(this.baseUrl, request);
  }

  getPostingTimeSuggestions(brandProfileId: string, platforms: string[]): Observable<ApiResponse<PostingTimeSuggestionDto[]>> {
    const params = platforms.map(p => `platforms=${encodeURIComponent(p)}`).join('&');
    return this.http.get<ApiResponse<PostingTimeSuggestionDto[]>>(
      `${this.baseUrl}/posting-time-suggestions?brandProfileId=${encodeURIComponent(brandProfileId)}&${params}`,
    );
  }

  /** Removes a post from the local list only — call after `cancel()` succeeds. */
  remove(id: string): void {
    this._posts.update(list => list.filter(p => p.id !== id));
  }

  clear(): void {
    this._posts.set([]);
  }
}
