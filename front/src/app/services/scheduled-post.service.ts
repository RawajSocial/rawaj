import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../model/auth.model';
import { PagedResult } from '../model/paged-result.model';
import { CampaignPlatform } from '../model/campaign.model';
import { BackendSocialPlatform } from '../model/content-item.model';
import { ScheduledPost, ScheduledPostSummary } from '../model/scheduled-post.model';
import { CampaignService } from './campaign.service';

const PLATFORM_MAP: Record<BackendSocialPlatform, CampaignPlatform> = {
  Instagram: 'instagram',
  Facebook: 'facebook',
  Tiktok: 'tiktok',
  Youtube: 'youtube',
  Twitter: 'x',
  Linkedin: 'linkedin',
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
      campaignId: s.campaignId ?? '',
      campaignName: campaign?.name ?? 'بدون حملة',
      platform: PLATFORM_MAP[s.platform] ?? 'instagram',
      // The scheduled-posts endpoint doesn't return the underlying content text — show the
      // publishing account as a stand-in until content is joined in from ContentItem.
      content: s.accountName,
      scheduledAt: s.scheduledAt,
      status: STATUS_MAP[s.status] ?? 'scheduled',
      estimatedReach: s.reach ?? undefined,
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

  /** No generic "update scheduled post" endpoint exists on the backend yet — this only
   *  mutates the local optimistic view (calendar's edit modal), it does not persist. */
  update(post: ScheduledPost): void {
    this._posts.update(list => list.map(p => (p.id === post.id ? post : p)));
  }

  /** Local-only removal (no delete endpoint yet). */
  remove(id: string): void {
    this._posts.update(list => list.filter(p => p.id !== id));
  }

  clear(): void {
    this._posts.set([]);
  }
}
